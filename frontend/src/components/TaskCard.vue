<script setup>
import { computed, ref, toRef } from 'vue'
import { useElapsedTime } from '../composables/useElapsedTime'

const props = defineProps({
  task: { type: Object, required: true },
  variant: { type: String, default: 'compact' },
  userId: { type: Number, required: true },
  isAdmin: { type: Boolean, default: false },
  members: { type: Array, default: () => [] }
})

const emit = defineEmits(['delete', 'update-assignee'])

const taskRef = toRef(props, 'task')
const { formatted } = useElapsedTime(taskRef)

const isFeatured = computed(() => props.variant === 'featured')
const isMyTask = computed(() =>
  props.task.assignedToUserId === props.userId || props.task.createdByUserId === props.userId
)
const showTime = computed(() => props.isAdmin || isMyTask.value)
const canDelete = computed(() => props.isAdmin || props.task.createdByUserId === props.userId)
const canDrag = computed(() => props.isAdmin || isMyTask.value)
const canChangeAssignee = computed(() => props.isAdmin || props.task.createdByUserId === props.userId)

const editingAssignee = ref(false)

function onAssigneeChange(e) {
  const newId = Number(e.target.value)
  emit('update-assignee', { taskId: props.task.id, assignedToUserId: newId })
  editingAssignee.value = false
}
</script>

<template>
  <div class="task-card" :class="[variant, { 'no-drag': !canDrag }]">
    <div class="task-card__header">
      <h3 class="task-card__title">{{ task.title }}</h3>
      <button v-if="canDelete" class="task-card__delete" title="Xóa" @click="$emit('delete', task.id)">×</button>
    </div>

    <p v-if="task.description" class="task-card__description">{{ task.description }}</p>

    <div class="task-card__assignee-row">
      <select
        v-if="editingAssignee"
        class="task-card__assignee-select"
        :value="task.assignedToUserId"
        @change="onAssigneeChange"
        @blur="editingAssignee = false"
      >
        <option v-for="m in members" :key="m.userId" :value="m.userId">{{ m.username }}</option>
      </select>
      <span
        v-else-if="task.assignedToUsername || canChangeAssignee"
        class="task-card__assignee"
        :class="{ 'task-card__assignee--editable': canChangeAssignee }"
        :title="canChangeAssignee ? 'Nhấn để thay đổi người thực hiện' : undefined"
        @click="canChangeAssignee && (editingAssignee = true)"
      >{{ task.assignedToUsername || '— chưa giao' }}</span>
    </div>

    <div v-if="showTime" class="task-card__footer">
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

.task-card:active { cursor: grabbing; }

.task-card.no-drag { cursor: default; }

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
  flex-shrink: 0;
}

.task-card__delete:hover {
  color: #ef4444;
  background: #fef2f2;
}

.task-card__assignee-row {
  margin-top: 6px;
}

.task-card__assignee {
  font-size: 0.78rem;
  color: #9ca3af;
}

.task-card__assignee::before { content: '\1F464\00A0'; }

.task-card__assignee--editable {
  cursor: pointer;
  border-bottom: 1px dashed #d1d5db;
}

.task-card__assignee--editable:hover { color: #6b7280; }

.task-card__assignee-select {
  font-size: 0.78rem;
  color: #374151;
  border: 1px solid #d1d5db;
  border-radius: 4px;
  padding: 2px 4px;
  background: #fff;
  width: 100%;
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

/* Featured (In Progress) cards */
.task-card.featured {
  padding: 22px 24px;
  border: 2px solid #f97316;
  box-shadow: 0 8px 24px rgba(249, 115, 22, 0.18);
  background: linear-gradient(180deg, #fff7ed 0%, #ffffff 60%);
}

.task-card.featured .task-card__title { font-size: 1.4rem; }
.task-card.featured .task-card__description { font-size: 1rem; }
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
