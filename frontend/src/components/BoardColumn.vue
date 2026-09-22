<script setup>
import draggable from 'vuedraggable'
import TaskCard from './TaskCard.vue'

const props = defineProps({
  title: { type: String, required: true },
  status: { type: String, required: true },
  tasks: { type: Array, required: true },
  variant: { type: String, default: 'compact' }
})

const emit = defineEmits(['task-moved', 'delete-task'])

function onChange(event) {
  if (event.added) {
    emit('task-moved', { id: event.added.element.id, status: props.status })
  }
}
</script>

<template>
  <section class="board-column" :class="`board-column--${status.toLowerCase()}`">
    <header class="board-column__header">
      <h2>{{ title }}</h2>
      <span class="board-column__count">{{ tasks.length }}</span>
    </header>

    <slot name="header-extra" />

    <draggable
      class="board-column__list"
      :list="tasks"
      :group="{ name: 'tasks', pull: true, put: true }"
      item-key="id"
      @change="onChange"
    >
      <template #item="{ element }">
        <TaskCard :task="element" :variant="variant" @delete="$emit('delete-task', $event)" />
      </template>
    </draggable>

    <p v-if="tasks.length === 0" class="board-column__empty">Không có công việc</p>
  </section>
</template>

<style scoped>
.board-column {
  background: #f9fafb;
  border-radius: 12px;
  padding: 16px;
  display: flex;
  flex-direction: column;
  min-height: 400px;
}

.board-column--inprogress {
  background: #fffbeb;
}

.board-column__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.board-column__header h2 {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
  color: #374151;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.board-column__count {
  background: #e5e7eb;
  color: #4b5563;
  font-size: 0.75rem;
  font-weight: 600;
  padding: 2px 8px;
  border-radius: 999px;
}

.board-column__list {
  display: flex;
  flex-direction: column;
  gap: 10px;
  flex: 1;
  min-height: 60px;
}

.board-column__empty {
  text-align: center;
  color: #9ca3af;
  font-size: 0.85rem;
  margin-top: 12px;
}
</style>
