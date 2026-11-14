<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { getSources } from '../api/reading'

const router = useRouter()
const sources = ref([])
const loading = ref(true)
const username = ref(localStorage.getItem('username') || '')

onMounted(async () => {
  try { sources.value = await getSources() }
  finally { loading.value = false }
})
</script>

<template>
  <div class="library">
    <header class="library__header">
      <div class="library__header-left">
        <button class="library__back" @click="router.push('/teams')">← Tasks</button>
        <h1>Thư viện</h1>
      </div>
      <span class="library__user">{{ username }}</span>
    </header>

    <main class="library__main">
      <div v-if="loading" class="library__loading">Đang tải...</div>

      <div v-else-if="sources.length === 0" class="library__empty">
        <p>Chưa có nguồn tài liệu nào.</p>
      </div>

      <div v-else class="library__grid">
        <button
          v-for="s in sources"
          :key="s.id"
          class="source-card"
          @click="router.push(`/library/${s.slug}`)"
        >
          <img v-if="s.coverUrl" :src="s.coverUrl" class="source-card__cover" alt="" />
          <div v-else class="source-card__cover source-card__cover--placeholder">📚</div>
          <div class="source-card__body">
            <h2>{{ s.title }}</h2>
            <p v-if="s.description" class="source-card__desc">{{ s.description }}</p>
            <span class="source-card__count">{{ s.documentCount }} bài</span>
          </div>
        </button>
      </div>
    </main>
  </div>
</template>

<style scoped>
.library {
  min-height: 100vh;
  background: #f9fafb;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
}
.library__header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 1rem 1.5rem;
  background: #fff;
  border-bottom: 1px solid #e5e7eb;
  position: sticky; top: 0; z-index: 10;
}
.library__header-left { display: flex; align-items: center; gap: 1rem; }
.library__header h1 { font-size: 1.1rem; font-weight: 700; color: #111827; }
.library__back {
  background: none; border: none; cursor: pointer;
  color: #6b7280; font-size: .85rem; padding: 4px 8px;
  border-radius: 6px;
}
.library__back:hover { background: #f3f4f6; color: #111827; }
.library__user { font-size: .85rem; color: #6b7280; }
.library__main { max-width: 900px; margin: 0 auto; padding: 2rem 1.5rem; }
.library__loading, .library__empty { color: #6b7280; text-align: center; padding: 4rem; }
.library__grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 1.25rem; }

.source-card {
  background: #fff; border: 1px solid #e5e7eb; border-radius: 10px;
  overflow: hidden; cursor: pointer; text-align: left;
  transition: box-shadow .15s, transform .15s;
  padding: 0;
}
.source-card:hover { box-shadow: 0 4px 16px rgba(0,0,0,.1); transform: translateY(-2px); }
.source-card__cover {
  width: 100%; height: 140px; object-fit: cover; display: block;
  background: #f3f4f6;
}
.source-card__cover--placeholder {
  display: flex; align-items: center; justify-content: center; font-size: 2.5rem;
}
.source-card__body { padding: 1rem; }
.source-card__body h2 { font-size: 1rem; font-weight: 700; color: #111827; margin-bottom: .4rem; line-height: 1.4; }
.source-card__desc { font-size: .82rem; color: #6b7280; margin-bottom: .6rem; line-height: 1.5;
  display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
.source-card__count { font-size: .75rem; color: #0f6fec; font-weight: 600; }

@media (max-width: 600px) {
  .library__grid { grid-template-columns: 1fr; }
  .library__main { padding: 1rem; }
}
</style>
