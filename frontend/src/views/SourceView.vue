<script setup>
import { onMounted, ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { getSource } from '../api/reading'

const router = useRouter()
const route  = useRoute()
const source  = ref(null)
const loading = ref(true)

onMounted(async () => {
  try { source.value = await getSource(route.params.slug) }
  finally { loading.value = false }
})

const progress = (doc) => doc.scrollPercent != null ? Math.round(doc.scrollPercent) : null
</script>

<template>
  <div class="source-page">
    <header class="source-page__header">
      <button class="source-page__back" @click="router.push('/library')">← Thư viện</button>
      <h1 v-if="source">{{ source.title }}</h1>
    </header>

    <div v-if="loading" class="source-page__loading">Đang tải...</div>

    <main v-else-if="source" class="source-page__main">
      <p v-if="source.description" class="source-page__desc">{{ source.description }}</p>

      <ul class="doc-list">
        <li
          v-for="doc in source.documents"
          :key="doc.id"
          class="doc-item"
          @click="router.push(`/library/${route.params.slug}/${doc.slug}`)"
        >
          <div class="doc-item__content">
            <span class="doc-item__title">{{ doc.title }}</span>
            <span v-if="doc.postDate" class="doc-item__date">{{ doc.postDate }}</span>
          </div>
          <div class="doc-item__right">
            <div v-if="progress(doc) != null" class="doc-item__progress">
              <div class="doc-item__progress-bar" :style="{ width: progress(doc) + '%' }"></div>
            </div>
            <span v-if="progress(doc) != null" class="doc-item__progress-label">
              {{ progress(doc) === 100 ? '✓' : progress(doc) + '%' }}
            </span>
          </div>
        </li>
      </ul>
    </main>
  </div>
</template>

<style scoped>
.source-page {
  min-height: 100vh;
  background: #f9fafb;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
}
.source-page__header {
  display: flex; align-items: center; gap: 1rem;
  padding: 1rem 1.5rem;
  background: #fff; border-bottom: 1px solid #e5e7eb;
  position: sticky; top: 0; z-index: 10;
}
.source-page__header h1 { font-size: 1.05rem; font-weight: 700; color: #111827; }
.source-page__back {
  background: none; border: none; cursor: pointer;
  color: #6b7280; font-size: .85rem; padding: 4px 8px; border-radius: 6px; white-space: nowrap;
}
.source-page__back:hover { background: #f3f4f6; color: #111827; }
.source-page__loading { color: #6b7280; text-align: center; padding: 4rem; }
.source-page__main { max-width: 740px; margin: 0 auto; padding: 1.5rem; }
.source-page__desc { color: #6b7280; font-size: .9rem; margin-bottom: 1.5rem; }

.doc-list { list-style: none; display: flex; flex-direction: column; gap: .5rem; }
.doc-item {
  display: flex; align-items: center; justify-content: space-between; gap: 1rem;
  background: #fff; border: 1px solid #e5e7eb; border-radius: 8px;
  padding: .9rem 1.1rem; cursor: pointer;
  transition: box-shadow .12s, border-color .12s;
}
.doc-item:hover { box-shadow: 0 2px 8px rgba(0,0,0,.08); border-color: #d1d5db; }
.doc-item__content { display: flex; flex-direction: column; gap: .2rem; flex: 1; min-width: 0; }
.doc-item__title { font-size: .92rem; font-weight: 600; color: #111827; line-height: 1.4; }
.doc-item__date { font-size: .75rem; color: #9ca3af; }
.doc-item__right { display: flex; align-items: center; gap: .5rem; flex-shrink: 0; }
.doc-item__progress {
  width: 60px; height: 4px; background: #e5e7eb; border-radius: 2px; overflow: hidden;
}
.doc-item__progress-bar { height: 100%; background: #0f6fec; border-radius: 2px; transition: width .3s; }
.doc-item__progress-label { font-size: .72rem; color: #6b7280; min-width: 24px; text-align: right; }

@media (max-width: 600px) {
  .source-page__main { padding: 1rem; }
  .doc-item { flex-direction: column; align-items: flex-start; }
  .doc-item__right { align-self: flex-end; }
}
</style>
