<script setup>
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Button from 'primevue/button'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import { createTask, deleteTask, fetchTasks, updateTaskStatus } from '../api/tasks'
import { fetchMembers } from '../api/teams'
import BoardColumn from '../components/BoardColumn.vue'

const route = useRoute()
const router = useRouter()

const teamId = Number(route.params.teamId)
const teamName = ref(localStorage.getItem('currentTeamName') || 'Team')
const isAdmin = ref(localStorage.getItem('currentTeamRole') === 'Admin')

const todoTasks = ref([])
const inProgressTasks = ref([])
const doneTasks = ref([])
const newTitle = ref('')
const loading = ref(true)
const errorMessage = ref('')
const username = ref(localStorage.getItem('username') || '')
const userId = Number(localStorage.getItem('userId'))
const members = ref([])
const selectedAssigneeId = ref(userId)

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
  } catch (err) {
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

async function handleAddTask() {
  const title = newTitle.value.trim()
  if (!title) return

  try {
    const created = await createTask(title, null, teamId, selectedAssigneeId.value)
    todoTasks.value.push(created)
    newTitle.value = ''
  } catch (err) {
    errorMessage.value = 'Không tạo được công việc mới.'
  }
}

async function handleTaskMoved({ id, status }) {
  const list = columnsByStatus[status].value
  const task = list.find((t) => t.id === id)
  if (!task) return

  try {
    const updated = await updateTaskStatus(id, status)
    Object.assign(task, updated)
  } catch (err) {
    errorMessage.value = 'Không cập nhật được trạng thái công việc.'
    await loadTasks()
  }
}

async function handleDeleteTask(id) {
  try {
    await deleteTask(id)
    ;[todoTasks, inProgressTasks, doneTasks].forEach((list) => {
      const index = list.value.findIndex((t) => t.id === id)
      if (index !== -1) list.value.splice(index, 1)
    })
  } catch (err) {
    errorMessage.value = 'Không xóa được công việc.'
  }
}

function goToTeams() {
  router.push('/teams')
}

function logout() {
  localStorage.clear()
  router.push('/login')
}

onMounted(async () => {
  await Promise.all([loadTasks(), loadMembers()])
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
        <span>{{ username }}</span>
        <Button label="Đăng xuất" text size="small" @click="logout" />
      </div>
    </header>

    <p v-if="errorMessage" class="board-page__error">{{ errorMessage }}</p>

    <div v-if="loading" class="board-page__loading">Đang tải...</div>

    <div v-else class="board-page__columns">
      <BoardColumn
        title="Cần làm"
        status="Todo"
        :tasks="todoTasks"
        :is-admin="isAdmin"
        variant="compact"
        @task-moved="handleTaskMoved"
        @delete-task="handleDeleteTask"
      >
        <template #header-extra>
          <form class="board-page__add-form" @submit.prevent="handleAddTask">
            <Textarea
              v-model="newTitle"
              placeholder="Thêm công việc mới..."
              class="board-page__add-input"
              rows="3"
              autoResize
              @keydown="(e) => (e.ctrlKey || e.metaKey) && e.key === 'Enter' && handleAddTask()"
            />
            <Select
              v-model="selectedAssigneeId"
              :options="members"
              optionLabel="username"
              optionValue="userId"
              placeholder="Giao cho..."
              class="board-page__assignee-select"
              size="small"
            />
            <Button label="Thêm" size="small" type="submit" />
          </form>
        </template>
      </BoardColumn>

      <BoardColumn
        title="Đang làm"
        status="InProgress"
        :tasks="inProgressTasks"
        :is-admin="isAdmin"
        variant="featured"
        @task-moved="handleTaskMoved"
        @delete-task="handleDeleteTask"
      />

      <BoardColumn
        title="Đã xong"
        status="Done"
        :tasks="doneTasks"
        :is-admin="isAdmin"
        variant="compact"
        @task-moved="handleTaskMoved"
        @delete-task="handleDeleteTask"
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
  margin-bottom: 20px;
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
.board-page__back:hover {
  color: #111827;
}
.board-page__header h1 {
  font-size: 1.5rem;
  color: #111827;
  margin: 0;
}
.board-page__user {
  display: flex;
  align-items: center;
  gap: 10px;
  color: #6b7280;
  font-size: 0.9rem;
}
.board-page__error {
  background: #fef2f2;
  color: #b91c1c;
  padding: 10px 14px;
  border-radius: 8px;
  margin-bottom: 16px;
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
