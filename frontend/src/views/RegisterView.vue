<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import { register } from '../api/auth'

const router = useRouter()
const username = ref('')
const fullName = ref('')
const password = ref('')
const confirmPassword = ref('')
const loading = ref(false)
const errorMessage = ref('')

async function handleRegister() {
  errorMessage.value = ''
  if (username.value.trim().length < 3) {
    errorMessage.value = 'Tên đăng nhập phải có ít nhất 3 ký tự.'
    return
  }
  if (!fullName.value.trim()) {
    errorMessage.value = 'Họ và tên không được để trống.'
    return
  }
  if (password.value.length < 6) {
    errorMessage.value = 'Mật khẩu phải có ít nhất 6 ký tự.'
    return
  }
  if (password.value !== confirmPassword.value) {
    errorMessage.value = 'Mật khẩu xác nhận không khớp.'
    return
  }
  loading.value = true
  try {
    const data = await register(username.value.trim(), fullName.value.trim(), password.value)
    localStorage.setItem('token', data.token)
    localStorage.setItem('username', data.username)
    localStorage.setItem('fullName', data.fullName)
    localStorage.setItem('userId', data.userId)
    router.push('/teams')
  } catch (err) {
    const msg = err.response?.data?.message
    errorMessage.value = msg || 'Đăng ký thất bại. Vui lòng thử lại.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="auth-page">
    <div class="auth-card">
      <h1 class="auth-card__title">Đăng ký</h1>
      <form @submit.prevent="handleRegister" class="auth-card__form">
        <div class="auth-card__field">
          <label>Tên đăng nhập</label>
          <InputText v-model="username" placeholder="Tối thiểu 3 ký tự" class="w-full" autocomplete="username" />
        </div>
        <div class="auth-card__field">
          <label>Họ và tên</label>
          <InputText v-model="fullName" placeholder="Nguyễn Văn A" class="w-full" autocomplete="name" />
        </div>
        <div class="auth-card__field">
          <label>Mật khẩu</label>
          <Password v-model="password" placeholder="Tối thiểu 6 ký tự" :feedback="false" toggleMask class="w-full" inputClass="w-full" :inputProps="{ autocomplete: 'new-password' }" />
        </div>
        <div class="auth-card__field">
          <label>Xác nhận mật khẩu</label>
          <Password v-model="confirmPassword" placeholder="Nhập lại mật khẩu" :feedback="false" toggleMask class="w-full" inputClass="w-full" :inputProps="{ autocomplete: 'confirm-password' }" />
        </div>
        <p v-if="errorMessage" class="auth-card__error">{{ errorMessage }}</p>
        <Button label="Đăng ký" type="submit" :loading="loading" class="w-full" />
      </form>
      <p class="auth-card__switch">Đã có tài khoản? <router-link to="/login">Đăng nhập</router-link></p>
    </div>
  </div>
</template>

<style scoped>
.auth-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f9fafb;
}
.auth-card {
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 4px 24px rgba(0,0,0,0.08);
  padding: 40px;
  width: 100%;
  max-width: 380px;
}
.auth-card__title {
  font-size: 1.4rem;
  font-weight: 700;
  color: #111827;
  margin: 0 0 24px;
  text-align: center;
}
.auth-card__form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.auth-card__field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.auth-card__field label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}
.auth-card__error {
  background: #fef2f2;
  color: #b91c1c;
  padding: 8px 12px;
  border-radius: 6px;
  font-size: 0.875rem;
  margin: 0;
}
.auth-card__switch {
  text-align: center;
  margin-top: 20px;
  font-size: 0.875rem;
  color: #6b7280;
}
.auth-card__switch a {
  color: #4f46e5;
  text-decoration: none;
  font-weight: 500;
}
</style>
