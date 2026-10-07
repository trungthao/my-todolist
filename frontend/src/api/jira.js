import http from './http'

export const fetchJiraIssue = (key) => http.get(`/jira/issues/${encodeURIComponent(key)}`).then(r => r.data)
