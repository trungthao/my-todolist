<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import { login } from '../api/auth'

const router = useRouter()

const username = ref('')
const password = ref('')
const errorMessage = ref('')
const submitting = ref(false)

async function handleSubmit() {
  errorMessage.value = ''
  submitting.value = true
  try {
    const result = await login(username.value, password.value)
    localStorage.setItem('token', result.token)
    localStorage.setItem('username', result.username)
    localStorage.setItem('fullName', result.fullName || result.username)
    localStorage.setItem('userId', result.userId)
    router.push('/teams')
  } catch (err) {
    errorMessage.value = err.response?.data?.message || 'Đăng nhập thất bại.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <form class="login-card" @submit.prevent="handleSubmit">
      <h1>Đăng nhập</h1>
      <p class="login-card__subtitle">Quản lý công việc cá nhân</p>

      <label class="login-card__field">
        <span>Tên đăng nhập</span>
        <InputText v-model="username" autofocus required />
      </label>

      <label class="login-card__field">
        <span>Mật khẩu</span>
        <Password v-model="password" :feedback="false" toggleMask required />
      </label>

      <p v-if="errorMessage" class="login-card__error">{{ errorMessage }}</p>

      <Button label="Đăng nhập" type="submit" class="login-card__submit" :loading="submitting" />
    </form>
    <p class="login-page__register">Chưa có tài khoản? <router-link to="/register">Đăng ký</router-link></p>
  </div>
</template>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: #f3f4f6;
}

.login-card {
  background: #ffffff;
  padding: 36px 40px;
  border-radius: 14px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.08);
  width: 340px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.login-card h1 {
  margin: 0;
  font-size: 1.4rem;
  color: #111827;
}

.login-card__subtitle {
  margin: -8px 0 6px;
  color: #6b7280;
  font-size: 0.9rem;
}

.login-card__field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 0.85rem;
  color: #374151;
}

.login-card__field :deep(input) {
  width: 100%;
}

.login-card__error {
  color: #b91c1c;
  font-size: 0.85rem;
  margin: 0;
}

.login-card__submit {
  margin-top: 6px;
}

.login-page__register {
  margin-top: 16px;
  font-size: 0.875rem;
  color: #6b7280;
}

.login-page__register a {
  color: #4f46e5;
  text-decoration: none;
  font-weight: 500;
}
</style>
