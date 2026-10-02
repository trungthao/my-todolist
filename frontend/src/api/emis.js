import http from './http'

export const setupEmisLogin = (username, twoFactorCode) =>
  http.post('/emis/login-setup', { username, twoFactorCode }).then(r => r.data)
