<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import { fetchMyTeams, createTeam } from '../api/teams'

const router = useRouter()
const teams = ref([])
const loading = ref(true)
const newTeamName = ref('')
const creating = ref(false)
const showForm = ref(false)
const errorMessage = ref('')
const username = ref(localStorage.getItem('username') || '')

async function load() {
  loading.value = true
  try {
    teams.value = await fetchMyTeams()
  } catch {
    errorMessage.value = 'Không tải được danh sách team.'
  } finally {
    loading.value = false
  }
}

async function handleCreate() {
  if (!newTeamName.value.trim()) return
  creating.value = true
  try {
    const team = await createTeam(newTeamName.value.trim())
    teams.value.push(team)
    newTeamName.value = ''
    showForm.value = false
    selectTeam(team)
  } catch (err) {
    errorMessage.value = err.response?.data?.message || 'Không tạo được team.'
  } finally {
    creating.value = false
  }
}

function selectTeam(team) {
  localStorage.setItem('currentTeamId', team.id)
  localStorage.setItem('currentTeamName', team.name)
  localStorage.setItem('currentTeamRole', team.myRole)
  router.push(`/board/${team.id}`)
}

function logout() {
  localStorage.clear()
  router.push('/login')
}

onMounted(load)
</script>

<template>
  <div class="teams-page">
    <header class="teams-page__header">
      <h1>Chọn team</h1>
      <div class="teams-page__user">
        <span>{{ username }}</span>
        <Button label="Đăng xuất" text size="small" @click="logout" />
      </div>
    </header>

    <p v-if="errorMessage" class="teams-page__error">{{ errorMessage }}</p>

    <div v-if="loading" class="teams-page__loading">Đang tải...</div>

    <div v-else class="teams-page__content">
      <div v-if="teams.length === 0 && !showForm" class="teams-page__empty">
        <p>Bạn chưa thuộc team nào.</p>
      </div>

      <div class="teams-page__list">
        <button
          v-for="team in teams"
          :key="team.id"
          class="team-card"
          @click="selectTeam(team)"
        >
          <div class="team-card__name">{{ team.name }}</div>
          <div class="team-card__meta">
            <span class="team-card__role" :class="team.myRole === 'Admin' ? 'team-card__role--admin' : ''">
              {{ team.myRole === 'Admin' ? 'Quản lý' : 'Thành viên' }}
            </span>
            <span class="team-card__count">{{ team.memberCount }} thành viên</span>
          </div>
        </button>
      </div>

      <div v-if="showForm" class="teams-page__create-form">
        <InputText v-model="newTeamName" placeholder="Tên team..." @keydown.enter="handleCreate" />
        <Button label="Tạo" size="small" :loading="creating" @click="handleCreate" />
        <Button label="Hủy" text size="small" @click="showForm = false" />
      </div>

      <Button
        v-else
        label="+ Tạo team mới"
        text
        size="small"
        class="teams-page__create-btn"
        @click="showForm = true"
      />
    </div>
  </div>
</template>

<style scoped>
.teams-page {
  max-width: 560px;
  margin: 0 auto;
  padding: 40px 24px;
}
.teams-page__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 32px;
}
.teams-page__header h1 {
  font-size: 1.4rem;
  font-weight: 700;
  color: #111827;
  margin: 0;
}
.teams-page__user {
  display: flex;
  align-items: center;
  gap: 8px;
  color: #6b7280;
  font-size: 0.875rem;
}
.teams-page__error {
  background: #fef2f2;
  color: #b91c1c;
  padding: 10px 14px;
  border-radius: 8px;
  margin-bottom: 16px;
}
.teams-page__loading {
  color: #6b7280;
  text-align: center;
  padding: 40px 0;
}
.teams-page__empty {
  color: #6b7280;
  text-align: center;
  padding: 24px 0;
}
.teams-page__list {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 20px;
}
.team-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border: 1px solid #e5e7eb;
  border-radius: 10px;
  background: #fff;
  cursor: pointer;
  text-align: left;
  transition: border-color 0.15s, box-shadow 0.15s;
  width: 100%;
  font-family: inherit;
}
.team-card:hover {
  border-color: #4f46e5;
  box-shadow: 0 2px 8px rgba(79,70,229,0.1);
}
.team-card__name {
  font-size: 1rem;
  font-weight: 600;
  color: #111827;
}
.team-card__meta {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 0.8rem;
  color: #6b7280;
}
.team-card__role {
  padding: 2px 8px;
  border-radius: 999px;
  background: #f3f4f6;
  color: #6b7280;
}
.team-card__role--admin {
  background: #ede9fe;
  color: #6d28d9;
}
.teams-page__create-form {
  display: flex;
  gap: 8px;
  align-items: center;
}
.teams-page__create-btn {
  margin-top: 4px;
}
</style>
