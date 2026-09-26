import http from './http'

export const fetchTasks = (teamId) => http.get('/tasks', { params: { teamId } }).then(r => r.data)
export const createTask = (title, description, teamId, assignedToUserId) =>
  http.post('/tasks', { title, description, teamId, assignedToUserId }).then(r => r.data)
export const updateTask = (id, title, description, assignedToUserId) =>
  http.put(`/tasks/${id}`, { title, description, assignedToUserId }).then(r => r.data)
export const updateTaskStatus = (id, status) =>
  http.put(`/tasks/${id}/status`, { status }).then(r => r.data)
export const deleteTask = (id) => http.delete(`/tasks/${id}`)
