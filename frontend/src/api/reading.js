import http from './http'

export const getSources = () => http.get('/reading/sources').then(r => r.data)
export const getSource = (slug) => http.get(`/reading/sources/${slug}`).then(r => r.data)
export const getDocumentContent = (id) => http.get(`/reading/documents/${id}/content`).then(r => r.data)

export const upsertProgress = (documentId, scrollPercent) =>
  http.put('/reading/progress', { documentId, scrollPercent })

export const getHighlights = (documentId) =>
  http.get(`/reading/highlights/${documentId}`).then(r => r.data)
export const createHighlight = (payload) =>
  http.post('/reading/highlights', payload).then(r => r.data)
export const deleteHighlight = (id) => http.delete(`/reading/highlights/${id}`)

export const getNotes = (documentId) =>
  http.get(`/reading/notes/${documentId}`).then(r => r.data)
export const createNote = (documentId, content) =>
  http.post('/reading/notes', { documentId, content }).then(r => r.data)
export const updateNote = (id, content) =>
  http.put(`/reading/notes/${id}`, { content }).then(r => r.data)
export const deleteNote = (id) => http.delete(`/reading/notes/${id}`)
