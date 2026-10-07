<script setup>
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useTaskHub } from '../composables/useTaskHub'
import Button from 'primevue/button'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import InputText from 'primevue/inputtext'
import { createTask, deleteTask, fetchTasks, updateTask, updateTaskAssignee, updateTaskStatus } from '../api/tasks'
import { addMember, fetchMembers, removeMember } from '../api/teams'
import { fetchJiraIssue } from '../api/jira'
import BoardColumn from '../components/BoardColumn.vue'

const route = useRoute()
const router = useRouter()

const teamId = Number(route.params.teamId)
const teamName = ref(localStorage.getItem('currentTeamName') || 'Team')
const isAdmin = ref(localStorage.getItem('currentTeamRole') === 'Admin')
const userId = Number(localStorage.getItem('userId'))
const isOwner = ref(Number(localStorage.getItem('currentTeamCreatedByUserId')) === userId)

const todoTasks = ref([])
const inProgressTasks = ref([])
const doneTasks = ref([])
const newTitle = ref('')
const loading = ref(true)
const errorMessage = ref('')
const username = ref(localStorage.getItem('fullName') || localStorage.getItem('username') || '')
const members = ref([])
const selectedAssigneeId = ref(userId)
const addingTask = ref(false)

const showMembers = ref(false)
const newMemberUsername = ref('')
const addingMember = ref(false)
const memberError = ref('')

const columnsByStatus = {
  Todo: todoTasks,
  InProgress: inProgressTasks,
  Done: doneTasks
}

function bucketize(tasks) {
  todoTasks.value = tasks.filter((t) => t.status === 'Todo')
  inProgressTasks.value = tasks.filter((t) => t.status === 'InProgress')
  doneTasks.value = tasks.filter((t) => t.status === 'Done')
}

async function loadTasks() {
  loading.value = true
  errorMessage.value = ''
  try {
    const tasks = await fetchTasks(teamId)
    bucketize(tasks)
  } catch {
    errorMessage.value = 'Không tải được danh sách công việc.'
  } finally {
    loading.value = false
  }
}

async function loadMembers() {
  try {
    members.value = await fetchMembers(teamId)
  } catch {}
}

// Input that is just a Jira key ("EMTT-3832") or browse link creates the task from the ticket.
const JIRA_INPUT = /^(?:https?:\/\/\S+\/browse\/)?([A-Za-z][A-Za-z0-9_]+-\d+)\/?$/

async function handleAddTask() {
  const input = newTitle.value.trim()
  if (!input || addingTask.value) return
  addingTask.value = true
  errorMessage.value = ''
  try {
    const jiraKey = input.match(JIRA_INPUT)?.[1]
    let title = input
    let description = null
    if (jiraKey) {
      try {
        ;({ title, description } = await fetchJiraIssue(jiraKey))
      } catch (err) {
        errorMessage.value = err.response?.data?.message || 'Không lấy được thông tin từ Jira.'
        return
      }
    }
    const created = await createTask(title, description, teamId, selectedAssigneeId.value)
    if (!todoTasks.value.some((t) => t.id === created.id)) todoTasks.value.push(created)
    newTitle.value = ''
  } catch {
    errorMessage.value = 'Không tạo được công việc mới.'
  } finally {
    addingTask.value = false
  }
}

async function handleTaskMoved({ id, status }) {
  const list = columnsByStatus[status].value
  const task = list.find((t) => t.id === id)
  if (!task) return
  try {
    const updated = await updateTaskStatus(id, status)
    Object.assign(task, updated)
  } catch {
    errorMessage.value = 'Không cập nhật được trạng thái công việc.'
    await loadTasks()
  }
}

async function handleUpdateAssignee({ taskId, assignedToUserId }) {
  const allTasks = [...todoTasks.value, ...inProgressTasks.value, ...doneTasks.value]
  const task = allTasks.find((t) => t.id === taskId)
  if (!task) return
  try {
    const updated = await updateTaskAssignee(taskId, assignedToUserId)
    Object.assign(task, updated)
  } catch (err) {
    errorMessage.value = err.response?.data?.message || 'Không cập nhật được người thực hiện.'
  }
}

async function handleUpdateTask({ taskId, title, description }) {
  const allTasks = [...todoTasks.value, ...inProgressTasks.value, ...doneTasks.value]
  const task = allTasks.find((t) => t.id === taskId)
  if (!task) return
  try {
    const updated = await updateTask(taskId, title, description, task.assignedToUserId)
    Object.assign(task, updated)
  } catch (err) {
    errorMessage.value = err.response?.data?.message || 'Không cập nhật được công việc.'
  }
}

async function handleDeleteTask(id) {
  try {
    await deleteTask(id)
    ;[todoTasks, inProgressTasks, doneTasks].forEach((list) => {
      const index = list.value.findIndex((t) => t.id === id)
      if (index !== -1) list.value.splice(index, 1)
    })
  } catch {
    errorMessage.value = 'Không xóa được công việc.'
  }
}

async function handleAddMember() {
  const uname = newMemberUsername.value.trim()
  if (!uname) return
  memberError.value = ''
  addingMember.value = true
  try {
    const m = await addMember(teamId, uname)
    members.value.push(m)
    newMemberUsername.value = ''
  } catch (err) {
    memberError.value = err.response?.data?.message || 'Không thêm được thành viên.'
  } finally {
    addingMember.value = false
  }
}

async function handleRemoveMember(targetUserId) {
  memberError.value = ''
  try {
    await removeMember(teamId, targetUserId)
    members.value = members.value.filter((m) => m.userId !== targetUserId)
  } catch (err) {
    memberError.value = err.response?.data?.message || 'Không xóa được thành viên.'
  }
}

function goToTeams() {
  router.push('/teams')
}

function logout() {
  localStorage.clear()
  router.push('/login')
}

const { start: startHub, stop: stopHub } = useTaskHub({
  onTaskCreated(task) {
    const alreadyExists = [...todoTasks.value, ...inProgressTasks.value, ...doneTasks.value]
      .some((t) => t.id === task.id)
    if (alreadyExists) return
    columnsByStatus[task.status]?.value.push(task)
  },
  onTaskUpdated(task) {
    for (const list of [todoTasks, inProgressTasks, doneTasks]) {
      const idx = list.value.findIndex((t) => t.id === task.id)
      if (idx !== -1) {
        if (list.value[idx].status === task.status) {
          list.value.splice(idx, 1, task)
        } else {
          list.value.splice(idx, 1)
          columnsByStatus[task.status]?.value.push(task)
        }
        return
      }
    }
  },
  onTaskDeleted({ id }) {
    for (const list of [todoTasks, inProgressTasks, doneTasks]) {
      const idx = list.value.findIndex((t) => t.id === id)
      if (idx !== -1) { list.value.splice(idx, 1); return }
    }
  }
})

onMounted(async () => {
  await Promise.all([loadTasks(), loadMembers()])
  await startHub(teamId)
})
</script>

<template>
  <div class="board-page">
    <header class="board-page__header">
      <div class="board-page__title-group">
        <button class="board-page__back" @click="goToTeams">← Teams</button>
        <h1>{{ teamName }}</h1>
      </div>
      <div class="board-page__user">
        <button class="members-btn" @click="showMembers = !showMembers">
          <span class="members-btn__icon">👥</span>
          {{ members.length }} thành viên
        </button>
        <span>{{ username }}</span>
        <Button label="Đăng xuất" text size="small" @click="logout" />
      </div>
    </header>

    <div v-if="showMembers" class="members-panel">
      <div class="members-panel__list">
        <div v-for="m in members" :key="m.userId" class="members-panel__row">
          <span class="members-panel__name">{{ m.fullName || m.username }}</span>
          <span class="members-panel__role" :class="m.role === 'Admin' ? 'members-panel__role--admin' : ''">
            {{ m.role === 'Admin' ? 'Quản lý' : 'Thành viên' }}
          </span>
          <button
            v-if="isOwner && m.userId !== userId"
            class="members-panel__remove"
            title="Xóa khỏi team"
            @click="handleRemoveMember(m.userId)"
          >×</button>
        </div>
      </div>
      <form v-if="isOwner" class="members-panel__add" @submit.prevent="handleAddMember">
        <InputText
          v-model="newMemberUsername"
          placeholder="Tên đăng nhập..."
          size="small"
        />
        <Button label="Mời" size="small" type="submit" :loading="addingMember" />
      </form>
      <p v-if="memberError" class="members-panel__error">{{ memberError }}</p>
    </div>

    <p v-if="errorMessage" class="board-page__error">{{ errorMessage }}</p>

    <div v-if="loading" class="board-page__loading">Đang tải...</div>

    <div v-else class="board-page__columns">
      <BoardColumn
        title="Cần làm"
        status="Todo"
        :tasks="todoTasks"
        :is-admin="isAdmin"
        :user-id="userId"
        :members="members"
        variant="compact"
        @task-moved="handleTaskMoved"
        @delete-task="handleDeleteTask"
        @update-assignee="handleUpdateAssignee"
        @update-task="handleUpdateTask"
      >
        <template #header-extra>
          <form class="board-page__add-form" @submit.prevent="handleAddTask">
            <Textarea
              v-model="newTitle"
              placeholder="Thêm công việc mới hoặc dán mã/link Jira..."
              class="board-page__add-input"
              rows="3"
              autoResize
              @keydown="(e) => (e.ctrlKey || e.metaKey) && e.key === 'Enter' && handleAddTask()"
            />
            <Select
              v-model="selectedAssigneeId"
              :options="members"
              :optionLabel="(m) => m.fullName || m.username"
              optionValue="userId"
              placeholder="Giao cho..."
              class="board-page__assignee-select"
              size="small"
            />
            <Button label="Thêm" size="small" type="submit" :loading="addingTask" />
          </form>
        </template>
      </BoardColumn>

      <BoardColumn
        title="Đang làm"
        status="InProgress"
        :tasks="inProgressTasks"
        :is-admin="isAdmin"
        :user-id="userId"
        :members="members"
        variant="featured"
        @task-moved="handleTaskMoved"
        @delete-task="handleDeleteTask"
        @update-assignee="handleUpdateAssignee"
        @update-task="handleUpdateTask"
      />

      <BoardColumn
        title="Đã xong"
        status="Done"
        :tasks="doneTasks"
        :is-admin="isAdmin"
        :user-id="userId"
        :members="members"
        variant="compact"
        @task-moved="handleTaskMoved"
        @delete-task="handleDeleteTask"
        @update-assignee="handleUpdateAssignee"
        @update-task="handleUpdateTask"
      />
    </div>
  </div>
</template>

<style scoped>
.board-page {
  max-width: 1200px;
  margin: 0 auto;
  padding: 24px 32px 24px;
  height: 100vh;
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
}

.board-page__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.board-page__title-group {
  display: flex;
  align-items: center;
  gap: 12px;
}

.board-page__back {
  background: none;
  border: none;
  color: #6b7280;
  font-size: 0.875rem;
  cursor: pointer;
  padding: 4px 0;
}

.board-page__back:hover { color: #111827; }

.board-page__header h1 {
  font-size: 1.5rem;
  color: #111827;
  margin: 0;
}

.board-page__user {
  display: flex;
  align-items: center;
  gap: 12px;
  color: #6b7280;
  font-size: 0.9rem;
}

.members-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  background: #f3f4f6;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 5px 12px;
  font-size: 0.85rem;
  color: #374151;
  cursor: pointer;
  transition: background 0.15s;
}

.members-btn:hover { background: #e5e7eb; }
.members-btn__icon { font-size: 1rem; }

/* Members panel */
.members-panel {
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 10px;
  padding: 14px 16px;
  margin-bottom: 12px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.members-panel__list {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.members-panel__row {
  display: flex;
  align-items: center;
  gap: 10px;
}

.members-panel__name {
  font-size: 0.9rem;
  color: #111827;
  min-width: 120px;
}

.members-panel__role {
  font-size: 0.75rem;
  padding: 2px 8px;
  border-radius: 999px;
  background: #f3f4f6;
  color: #6b7280;
}

.members-panel__role--admin {
  background: #ede9fe;
  color: #6d28d9;
}

.members-panel__remove {
  margin-left: auto;
  background: none;
  border: none;
  color: #9ca3af;
  font-size: 1.1rem;
  cursor: pointer;
  padding: 2px 6px;
  border-radius: 4px;
  line-height: 1;
}

.members-panel__remove:hover {
  color: #ef4444;
  background: #fef2f2;
}

.members-panel__add {
  display: flex;
  gap: 8px;
  align-items: center;
  padding-top: 6px;
  border-top: 1px solid #f3f4f6;
}

.members-panel__add :deep(input) { flex: 1; }

.members-panel__error {
  color: #b91c1c;
  font-size: 0.85rem;
  margin: 0;
}

.board-page__error {
  background: #fef2f2;
  color: #b91c1c;
  padding: 10px 14px;
  border-radius: 8px;
  margin-bottom: 12px;
}

.board-page__loading {
  color: #6b7280;
  padding: 40px 0;
  text-align: center;
}

.board-page__columns {
  display: grid;
  grid-template-columns: 1fr 1.5fr 1fr;
  gap: 20px;
  align-items: stretch;
  flex: 1;
  min-height: 0;
}

.board-page__add-form {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 14px;
}

.board-page__add-input {
  width: 100%;
  resize: vertical;
}

.board-page__assignee-select {
  width: 100%;
}
</style>
