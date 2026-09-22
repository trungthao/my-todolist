<script setup>
import { computed, toRef } from 'vue'
import { useElapsedTime } from '../composables/useElapsedTime'

const props = defineProps({
  task: { type: Object, required: true },
  variant: { type: String, default: 'compact' } // 'compact' | 'featured'
})

defineEmits(['delete'])

const taskRef = toRef(props, 'task')
const { formatted } = useElapsedTime(taskRef)

const isFeatured = computed(() => props.variant === 'featured')
</script>

<template>
  <div class="task-card" :class="variant">
    <div class="task-card__header">
      <h3 class="task-card__title">{{ task.title }}</h3>
      <button class="task-card__delete" title="Xóa" @click="$emit('delete', task.id)">×</button>
    </div>
    <p v-if="task.description" class="task-card__description">{{ task.description }}</p>
    <div class="task-card__footer">
      <span class="task-card__timer" :class="{ live: isFeatured }">
        <span v-if="isFeatured" class="task-card__dot"></span>
        {{ formatted }}
      </span>
    </div>
  </div>
</template>

<style scoped>
.task-card {
  background: #ffffff;
  border-radius: 10px;
  border: 1px solid #e5e7eb;
  padding: 12px 14px;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
  cursor: grab;
}

.task-card:active {
  cursor: grabbing;
}

.task-card__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px;
}

.task-card__title {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 600;
  color: #1f2937;
  word-break: break-word;
}

.task-card__description {
  margin: 6px 0 0;
  font-size: 0.85rem;
  color: #6b7280;
  word-break: break-word;
}

.task-card__delete {
  border: none;
  background: transparent;
  color: #9ca3af;
  font-size: 1.1rem;
  line-height: 1;
  cursor: pointer;
  padding: 2px 4px;
  border-radius: 6px;
}

.task-card__delete:hover {
  color: #ef4444;
  background: #fef2f2;
}

.task-card__footer {
  margin-top: 10px;
  display: flex;
  justify-content: flex-end;
}

.task-card__timer {
  font-size: 0.8rem;
  color: #6b7280;
  font-variant-numeric: tabular-nums;
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

/* Featured (In Progress) cards: biggest, boldest, most eye-catching */
.task-card.featured {
  padding: 22px 24px;
  border: 2px solid #f97316;
  box-shadow: 0 8px 24px rgba(249, 115, 22, 0.18);
  background: linear-gradient(180deg, #fff7ed 0%, #ffffff 60%);
}

.task-card.featured .task-card__title {
  font-size: 1.4rem;
}

.task-card.featured .task-card__description {
  font-size: 1rem;
}

.task-card.featured .task-card__timer {
  font-size: 1.15rem;
  font-weight: 700;
  color: #c2410c;
}

.task-card__dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #f97316;
  animation: pulse 1.4s infinite ease-in-out;
}

@keyframes pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.4; transform: scale(0.8); }
}
</style>
