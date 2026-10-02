<script setup>
import { computed, ref } from 'vue'
import ContextMenu from 'primevue/contextmenu'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import { linkify } from '../utils/linkify'
import { setupEmisLogin } from '../api/emis'

const props = defineProps({
  text: { type: String, default: '' },
  tag: { type: String, default: 'span' }
})

const segments = computed(() => linkify(props.text))

const menu = ref()
const activeSegment = ref(null)
const copiedText = ref(null)

const COPY_LABELS = {
  url: 'Sao chép liên kết',
  email: 'Sao chép email',
  phone: 'Sao chép số điện thoại'
}

const menuItems = computed(() => {
  const seg = activeSegment.value
  if (!seg) return []
  const items = [{ label: COPY_LABELS[seg.type], icon: 'pi pi-copy', command: () => copy(seg.text) }]
  if (seg.type === 'url') {
    items.push({ label: 'Mở trong tab mới', icon: 'pi pi-external-link', command: () => open(seg.href) })
  } else {
    items.push({ label: 'Thiết lập login', icon: 'pi pi-key', command: () => openLoginSetup(seg.text) })
  }
  return items
})

const loginSetup = ref({ visible: false, username: '', code: '', loading: false, error: '', success: false })

function openLoginSetup(username) {
  loginSetup.value = { visible: true, username, code: '', loading: false, error: '', success: false }
}

async function submitLoginSetup() {
  const state = loginSetup.value
  const code = state.code.trim()
  if (!code || state.loading) return
  state.loading = true
  state.error = ''
  state.success = false
  try {
    await setupEmisLogin(state.username, code)
    state.success = true
  } catch (err) {
    const data = err.response?.data
    state.error = [data?.message || 'Thiết lập login thất bại.', data?.detail?.slice(0, 300)]
      .filter(Boolean)
      .join(' ')
  } finally {
    state.loading = false
  }
}

function onContextMenu(event, seg) {
  activeSegment.value = seg
  menu.value.show(event)
}

function open(href) {
  window.open(href, '_blank', 'noopener,noreferrer')
}

async function copy(text) {
  try {
    await navigator.clipboard.writeText(text)
  } catch {
    // navigator.clipboard requires a secure context; fall back for plain-HTTP deployments.
    const el = document.createElement('textarea')
    el.value = text
    el.style.position = 'fixed'
    el.style.opacity = '0'
    document.body.appendChild(el)
    el.select()
    document.execCommand('copy')
    el.remove()
  }
  copiedText.value = text
  setTimeout(() => {
    if (copiedText.value === text) copiedText.value = null
  }, 1500)
}
</script>

<template>
  <component :is="tag" class="linkified">
    <template v-for="(seg, i) in segments" :key="i">
      <template v-if="seg.type === 'text'">{{ seg.text }}</template>
      <a
        v-else
        class="linkified__link"
        :class="{ 'linkified__link--copied': copiedText === seg.text }"
        :href="seg.href"
        :target="seg.type === 'url' ? '_blank' : undefined"
        :rel="seg.type === 'url' ? 'noopener noreferrer' : undefined"
        draggable="false"
        @click.stop
        @contextmenu.prevent.stop="onContextMenu($event, seg)"
      >{{ seg.text }}</a>
    </template>
    <ContextMenu ref="menu" :model="menuItems" />
    <Dialog
      v-model:visible="loginSetup.visible"
      modal
      header="Thiết lập login"
      :style="{ width: '22rem' }"
      :dismissableMask="!loginSetup.loading"
    >
      <form class="login-setup" @submit.prevent="submitLoginSetup">
        <div class="login-setup__field">
          <label>Tài khoản</label>
          <strong>{{ loginSetup.username }}</strong>
        </div>
        <div class="login-setup__field">
          <label for="login-setup-code">Mã xác thực 2 lớp</label>
          <InputText
            id="login-setup-code"
            v-model="loginSetup.code"
            inputmode="numeric"
            autocomplete="one-time-code"
            placeholder="VD: 744035"
            autofocus
            :disabled="loginSetup.loading"
          />
        </div>
        <p v-if="loginSetup.error" class="login-setup__error">{{ loginSetup.error }}</p>
        <p v-if="loginSetup.success" class="login-setup__success">Thiết lập login thành công.</p>
        <div class="login-setup__actions">
          <Button label="Đóng" text size="small" type="button" @click="loginSetup.visible = false" />
          <Button
            label="Thiết lập"
            size="small"
            type="submit"
            :loading="loginSetup.loading"
            :disabled="!loginSetup.code.trim()"
          />
        </div>
      </form>
    </Dialog>
  </component>
</template>

<style scoped>
.linkified {
  white-space: pre-wrap;
}

.linkified__link {
  color: #2563eb;
  text-decoration: underline;
  text-underline-offset: 2px;
  cursor: pointer;
  position: relative;
}

.linkified__link:hover { color: #1d4ed8; }

.linkified__link--copied::after {
  content: 'Đã sao chép';
  position: absolute;
  left: 50%;
  bottom: calc(100% + 4px);
  transform: translateX(-50%);
  background: #111827;
  color: #fff;
  font-size: 0.7rem;
  font-weight: 500;
  padding: 2px 6px;
  border-radius: 4px;
  white-space: nowrap;
  pointer-events: none;
}
.login-setup {
  display: flex;
  flex-direction: column;
  gap: 12px;
  white-space: normal;
}

.login-setup__field {
  display: flex;
  flex-direction: column;
  gap: 4px;
  font-size: 0.9rem;
}

.login-setup__field label {
  font-size: 0.8rem;
  color: #6b7280;
}

.login-setup__error,
.login-setup__success {
  margin: 0;
  font-size: 0.85rem;
  padding: 8px 10px;
  border-radius: 6px;
  word-break: break-word;
}

.login-setup__error { background: #fef2f2; color: #b91c1c; }
.login-setup__success { background: #f0fdf4; color: #15803d; }

.login-setup__actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
