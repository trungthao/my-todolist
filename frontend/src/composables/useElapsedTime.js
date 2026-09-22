import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { formatDuration } from '../utils/formatDuration'

// The backend always stores/sends UTC instants, but depending on the DB driver
// the JSON may or may not already carry a trailing 'Z'. Normalize before parsing.
function parseUtc(isoString) {
  const normalized = isoString.endsWith('Z') ? isoString : `${isoString}Z`
  return new Date(normalized).getTime()
}

export function useElapsedTime(task) {
  const now = ref(Date.now())
  let intervalId = null

  function stopTicking() {
    if (intervalId) {
      clearInterval(intervalId)
      intervalId = null
    }
  }

  function startTicking() {
    if (intervalId) return
    now.value = Date.now()
    intervalId = setInterval(() => {
      now.value = Date.now()
    }, 1000)
  }

  watch(
    () => [task.value.status, task.value.currentStartedAtUtc],
    ([status, startedAt]) => {
      if (status === 'InProgress' && startedAt) {
        startTicking()
      } else {
        stopTicking()
      }
    },
    { immediate: true }
  )

  onBeforeUnmount(stopTicking)

  const elapsedSeconds = computed(() => {
    const t = task.value
    let seconds = t.accumulatedSeconds
    if (t.status === 'InProgress' && t.currentStartedAtUtc) {
      const startedAtMs = parseUtc(t.currentStartedAtUtc)
      seconds += (now.value - startedAtMs) / 1000
    }
    return seconds
  })

  const formatted = computed(() => formatDuration(elapsedSeconds.value))

  return { elapsedSeconds, formatted }
}
