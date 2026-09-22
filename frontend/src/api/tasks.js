import http from './http'

export function fetchTasks() {
  return http.get('/tasks').then((res) => res.data)
}

export function createTask(title, description) {
  return http.post('/tasks', { title, description }).then((res) => res.data)
}

export function updateTask(id, title, description) {
  return http.put(`/tasks/${id}`, { title, description }).then((res) => res.data)
}

export function updateTaskStatus(id, status) {
  return http.put(`/tasks/${id}/status`, { status }).then((res) => res.data)
}

export function deleteTask(id) {
  return http.delete(`/tasks/${id}`)
}
