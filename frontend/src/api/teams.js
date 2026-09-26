import http from './http'

export const fetchMyTeams = () => http.get('/teams').then(r => r.data)
export const createTeam = (name) => http.post('/teams', { name }).then(r => r.data)
export const fetchMembers = (teamId) => http.get(`/teams/${teamId}/members`).then(r => r.data)
export const addMember = (teamId, username) => http.post(`/teams/${teamId}/members`, { username }).then(r => r.data)
export const removeMember = (teamId, userId) => http.delete(`/teams/${teamId}/members/${userId}`)
export const changeMemberRole = (teamId, userId, role) => http.put(`/teams/${teamId}/members/${userId}/role`, { role })
