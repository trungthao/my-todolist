import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { onUnmounted } from 'vue'

export function useTaskHub({ onTaskCreated, onTaskUpdated, onTaskDeleted }) {
  let connection = null

  async function start(teamId) {
    const token = localStorage.getItem('token')
    connection = new HubConnectionBuilder()
      .withUrl('/taskHub', { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('TaskCreated', onTaskCreated)
    connection.on('TaskUpdated', onTaskUpdated)
    connection.on('TaskDeleted', onTaskDeleted)

    await connection.start()
    await connection.invoke('JoinTeam', teamId)
  }

  async function stop(teamId) {
    if (!connection) return
    if (teamId != null) {
      try { await connection.invoke('LeaveTeam', teamId) } catch {}
    }
    await connection.stop()
    connection = null
  }

  onUnmounted(() => stop(null))

  return { start, stop }
}
