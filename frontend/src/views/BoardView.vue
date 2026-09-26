<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import Textarea from 'primevue/textarea'
import { createTask, deleteTask, fetchTasks, updateTaskStatus } from '../api/tasks'
import BoardColumn from '../components/BoardColumn.vue'

const router = useRouter()

const todoTasks = ref([])
const inProgressTasks = ref([])
const doneTasks = ref([])
const newTitle = ref('')
const loading = ref(true)
const errorMessage = ref('')
const username = ref(localStorage.getItem('username') || '')

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
    const tasks = await fetchTasks()
    bucketize(tasks)
  } catch (err) {
    errorMessage.value = 'Không tải được danh sách công việc.'
  } finally {
    loading.value = false
  }
}

async function handleAddTask() {
  const title = newTitle.value.trim()
  if (!title) return

  try {
    const created = await createTask(title, null)
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

function logout() {
  localStorage.removeItem('token')
  localStorage.removeItem('username')
  router.push('/login')
}

onMounted(loadTasks)
</script>

<template>
  <div class="board-page">
    <header class="board-page__header">
      <h1>Công việc của tôi</h1>
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
        variant="compact"
        @task-moved="handleTaskMoved"
        @delete-task="handleDeleteTask"
      >
        <template #header-extra>
          <form class="board-page__add-form" @submit.prevent="handleAddTask">
            <Textarea v-model="newTitle" placeholder="Thêm công việc mới..." class="board-page__add-input" rows="3" autoResize />
            <Button label="Thêm" size="small" type="submit" />
          </form>
        </template>
      </BoardColumn>

      <BoardColumn
        title="Đang làm"
        status="InProgress"
        :tasks="inProgressTasks"
        variant="featured"
        @task-moved="handleTaskMoved"
        @delete-task="handleDeleteTask"
      />

      <BoardColumn
        title="Đã xong"
        status="Done"
        :tasks="doneTasks"
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
</style>
