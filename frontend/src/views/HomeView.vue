<script setup>
import { useRouter } from 'vue-router'

const router  = useRouter()
const username = localStorage.getItem('username') || ''

const apps = [
  {
    id:    'tasks',
    icon:  '✅',
    label: 'Công việc',
    desc:  'Kanban, team tasks',
    route: '/teams',
    color: '#e8f1fd',
    accent: '#0f6fec',
  },
  {
    id:    'library',
    icon:  '📚',
    label: 'Thư viện',
    desc:  'Đọc tài liệu, highlight, note',
    route: '/library',
    color: '#f0fdf4',
    accent: '#16a34a',
  },
]

function logout() {
  localStorage.clear()
  router.push('/login')
}
</script>

<template>
  <div class="home">
    <header class="home__header">
      <div class="home__logo">⚡ Workspace</div>
      <div class="home__user">
        <span>{{ username }}</span>
        <button class="home__logout" @click="logout">Đăng xuất</button>
      </div>
    </header>

    <main class="home__main">
      <p class="home__greeting">Xin chào, <strong>{{ username }}</strong></p>

      <div class="home__grid">
        <button
          v-for="app in apps"
          :key="app.id"
          class="app-card"
          :style="{ '--bg': app.color, '--accent': app.accent }"
          @click="router.push(app.route)"
        >
          <span class="app-card__icon">{{ app.icon }}</span>
          <span class="app-card__label">{{ app.label }}</span>
          <span class="app-card__desc">{{ app.desc }}</span>
        </button>
      </div>
    </main>
  </div>
</template>

<style scoped>
.home {
  min-height: 100vh;
  background: #f8fafc;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
}

/* ── Header ─────────────────────────────────────────────────── */
.home__header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 1rem 1.5rem;
  background: #fff;
  border-bottom: 1px solid #e5e7eb;
}
.home__logo {
  font-size: 1.1rem; font-weight: 800; color: #111827; letter-spacing: -.02em;
}
.home__user {
  display: flex; align-items: center; gap: .75rem;
  font-size: .85rem; color: #6b7280;
}
.home__logout {
  background: none; border: 1px solid #e5e7eb; border-radius: 6px;
  padding: 4px 10px; font-size: .8rem; cursor: pointer; color: #6b7280;
}
.home__logout:hover { background: #f3f4f6; color: #111827; }

/* ── Main ───────────────────────────────────────────────────── */
.home__main {
  max-width: 600px; margin: 0 auto; padding: 3rem 1.5rem;
}
.home__greeting {
  font-size: 1.1rem; color: #374151; margin-bottom: 2rem;
}

/* ── App grid ───────────────────────────────────────────────── */
.home__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 1rem;
}

.app-card {
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  gap: .5rem; padding: 2rem 1rem;
  background: var(--bg);
  border: 1.5px solid color-mix(in srgb, var(--accent) 20%, transparent);
  border-radius: 16px;
  cursor: pointer;
  transition: transform .15s, box-shadow .15s;
  text-align: center;
}
.app-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 8px 24px color-mix(in srgb, var(--accent) 15%, transparent);
}
.app-card:active { transform: translateY(0); }

.app-card__icon  { font-size: 2.5rem; line-height: 1; }
.app-card__label { font-size: 1rem; font-weight: 700; color: #111827; }
.app-card__desc  { font-size: .78rem; color: #6b7280; }

/* ── Mobile ─────────────────────────────────────────────────── */
@media (max-width: 480px) {
  .home__grid { grid-template-columns: repeat(2, 1fr); }
  .home__main { padding: 2rem 1rem; }
}
</style>
