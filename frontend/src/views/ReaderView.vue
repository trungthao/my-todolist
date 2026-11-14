<script setup>
import { ref, onMounted, onUnmounted, nextTick, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  getSource, getDocumentContent,
  upsertProgress, getHighlights, createHighlight, deleteHighlight,
  getNotes, createNote, updateNote, deleteNote,
} from '../api/reading'

// ── route / state ─────────────────────────────────────────────────────────
const route  = useRoute()
const router = useRouter()

const source    = ref(null)
const doc       = ref(null)
const html      = ref('')
const loading   = ref(true)
const error     = ref('')

const sidebarOpen = ref(window.innerWidth > 768)
const notesOpen   = ref(false)
const tocItems    = ref([])
const activeToc   = ref('')
const readPercent = ref(0)

// highlights
const highlights    = ref([])
const hlToolbar     = ref(null)
const hlToolbarPos  = ref({ top: 0, left: 0 })
const hlToolbarVis  = ref(false)
const pendingRange  = ref(null)

// notes
const notes      = ref([])
const noteInput  = ref('')
const noteSaving = ref(false)
const editingNote = ref(null)
const editContent = ref('')

const articleEl = ref(null)

// ── load ──────────────────────────────────────────────────────────────────
onMounted(async () => {
  try {
    source.value = await getSource(route.params.sourceSlug)
    const found = source.value.documents.find(d => d.slug === route.params.docSlug)
    if (!found) { error.value = 'Không tìm thấy bài viết.'; return }
    doc.value = found

    const [content, hls, nts] = await Promise.all([
      getDocumentContent(found.id),
      getHighlights(found.id),
      getNotes(found.id),
    ])
    html.value = content
    highlights.value = hls
    notes.value = nts

    await nextTick()
    buildToc()
    restoreHighlights()
    setupScrollTracking()
    restoreScrollPosition()
  } catch (e) {
    error.value = 'Không tải được bài viết.'
  } finally {
    loading.value = false
  }
})

onUnmounted(() => {
  document.removeEventListener('mouseup', onMouseUp)
  document.removeEventListener('touchend', onMouseUp)
  window.removeEventListener('scroll', onScroll)
})

// ── TOC ───────────────────────────────────────────────────────────────────
function buildToc() {
  if (!articleEl.value) return
  const headings = articleEl.value.querySelectorAll('h2, h3')
  const items = []
  headings.forEach((h, i) => {
    const id = `h-${i}`
    h.id = id
    items.push({ id, text: h.textContent.trim(), level: h.tagName })
  })
  tocItems.value = items
}

// ── Scroll tracking ────────────────────────────────────────────────────────
let saveTimer = null
function setupScrollTracking() {
  window.addEventListener('scroll', onScroll, { passive: true })
  document.addEventListener('mouseup', onMouseUp)
  document.addEventListener('touchend', onMouseUp)
}

function onScroll() {
  // progress bar
  const scrolled = window.scrollY
  const total = document.documentElement.scrollHeight - window.innerHeight
  readPercent.value = total > 0 ? Math.min(100, Math.round((scrolled / total) * 100)) : 0

  // active TOC heading
  if (tocItems.value.length) {
    const visible = tocItems.value.filter(item => {
      const el = document.getElementById(item.id)
      if (!el) return false
      return el.getBoundingClientRect().top < 120
    })
    if (visible.length) activeToc.value = visible[visible.length - 1].id
  }

  // save progress (debounced 2s)
  clearTimeout(saveTimer)
  saveTimer = setTimeout(() => {
    if (doc.value) upsertProgress(doc.value.id, readPercent.value).catch(() => {})
  }, 2000)
}

function restoreScrollPosition() {
  if (doc.value?.scrollPercent != null && doc.value.scrollPercent > 0 && doc.value.scrollPercent < 99) {
    nextTick(() => {
      const total = document.documentElement.scrollHeight - window.innerHeight
      window.scrollTo({ top: (doc.value.scrollPercent / 100) * total, behavior: 'instant' })
    })
  }
}

function scrollToToc(id) {
  const el = document.getElementById(id)
  if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' })
  if (window.innerWidth <= 768) sidebarOpen.value = false
}

// ── Highlights ────────────────────────────────────────────────────────────
function getNodePath(node, root) {
  const path = []
  while (node && node !== root) {
    const parent = node.parentNode
    if (!parent) break
    path.unshift(Array.from(parent.childNodes).indexOf(node))
    node = parent
  }
  return path.join(',')
}

function getNodeByPath(pathStr, root) {
  if (!pathStr) return root
  return pathStr.split(',').reduce((n, i) => n?.childNodes[+i] ?? null, root)
}

function serializeRange(range) {
  const root = articleEl.value
  return JSON.stringify({
    s: getNodePath(range.startContainer, root),
    so: range.startOffset,
    e: getNodePath(range.endContainer, root),
    eo: range.endOffset,
  })
}

function applyHighlight(rangeJson, color, hlId) {
  const root = articleEl.value
  if (!root) return
  try {
    const data = JSON.parse(rangeJson)
    const startNode = getNodeByPath(data.s, root)
    const endNode   = getNodeByPath(data.e, root)
    if (!startNode || !endNode) return

    const range = document.createRange()
    range.setStart(startNode, data.so)
    range.setEnd(endNode, data.eo)

    const mark = document.createElement('mark')
    mark.dataset.hlId = hlId
    mark.className = `hl hl-${color}`
    mark.addEventListener('click', () => removeHighlightById(hlId))
    try {
      range.surroundContents(mark)
    } catch {
      const frag = range.extractContents()
      mark.appendChild(frag)
      range.insertNode(mark)
    }
  } catch {}
}

// Apply all saved highlights — from last to first to preserve offsets
function restoreHighlights() {
  ;[...highlights.value].reverse().forEach(hl => {
    applyHighlight(hl.rangeJson, hl.color, hl.id)
  })
}

function onMouseUp(e) {
  // hide toolbar if click on it
  if (hlToolbar.value?.contains(e.target)) return

  const sel = window.getSelection()
  if (!sel || sel.isCollapsed || !sel.rangeCount) {
    hlToolbarVis.value = false
    pendingRange.value = null
    return
  }

  const range = sel.getRangeAt(0)
  if (!articleEl.value?.contains(range.commonAncestorContainer)) {
    hlToolbarVis.value = false
    return
  }

  const rect = range.getBoundingClientRect()
  pendingRange.value = range.cloneRange()

  const toolbar = hlToolbar.value
  const tW = toolbar ? toolbar.offsetWidth : 160
  hlToolbarPos.value = {
    top:  rect.top + window.scrollY - 46,
    left: Math.max(8, rect.left + rect.width / 2 - tW / 2),
  }
  hlToolbarVis.value = true
}

async function pickColor(color) {
  if (!pendingRange.value || !doc.value) return
  const rangeJson  = serializeRange(pendingRange.value)
  const selectedText = pendingRange.value.toString().trim().slice(0, 500)
  hlToolbarVis.value = false

  try {
    const hl = await createHighlight({ documentId: doc.value.id, rangeJson, selectedText, color })
    highlights.value.push(hl)
    applyHighlight(rangeJson, color, hl.id)
    window.getSelection()?.removeAllRanges()
  } catch {}
  pendingRange.value = null
}

async function removeHighlightById(id) {
  try {
    await deleteHighlight(id)
    highlights.value = highlights.value.filter(h => h.id !== id)
    const mark = articleEl.value?.querySelector(`mark[data-hl-id="${id}"]`)
    if (mark) {
      const parent = mark.parentNode
      while (mark.firstChild) parent.insertBefore(mark.firstChild, mark)
      parent.removeChild(mark)
    }
  } catch {}
}

// ── Notes ─────────────────────────────────────────────────────────────────
async function addNote() {
  if (!noteInput.value.trim() || !doc.value) return
  noteSaving.value = true
  try {
    const n = await createNote(doc.value.id, noteInput.value)
    notes.value.unshift(n)
    noteInput.value = ''
  } finally {
    noteSaving.value = false }
}

function startEdit(note) {
  editingNote.value = note.id
  editContent.value = note.content
}

async function saveEdit(id) {
  try {
    const updated = await updateNote(id, editContent.value)
    const idx = notes.value.findIndex(n => n.id === id)
    if (idx !== -1) notes.value[idx] = updated
  } finally { editingNote.value = null }
}

async function removeNote(id) {
  await deleteNote(id)
  notes.value = notes.value.filter(n => n.id !== id)
}

// ── navigation ────────────────────────────────────────────────────────────
const currentIdx = computed(() => source.value?.documents.findIndex(d => d.id === doc.value?.id) ?? -1)
const prevDoc = computed(() => currentIdx.value > 0 ? source.value.documents[currentIdx.value - 1] : null)
const nextDoc = computed(() => currentIdx.value < (source.value?.documents.length ?? 0) - 1
  ? source.value.documents[currentIdx.value + 1] : null)

function goTo(d) {
  router.push(`/library/${route.params.sourceSlug}/${d.slug}`)
}
</script>

<template>
  <div class="reader">
    <!-- Header -->
    <header class="rd-header">
      <button class="rd-icon-btn" @click="sidebarOpen = !sidebarOpen" title="Toggle TOC">☰</button>
      <div class="rd-breadcrumb">
        <button class="rd-link" @click="router.push('/library')">Thư viện</button>
        <span> / </span>
        <button class="rd-link" @click="router.push(`/library/${route.params.sourceSlug}`)">
          {{ source?.title }}
        </button>
      </div>
      <div class="rd-header-actions">
        <button
          class="rd-icon-btn"
          :class="{ active: notesOpen }"
          @click="notesOpen = !notesOpen"
          title="Ghi chú"
        >📝 <span class="rd-badge" v-if="notes.length">{{ notes.length }}</span></button>
        <span class="rd-percent">{{ readPercent }}%</span>
      </div>
    </header>

    <!-- Progress -->
    <div class="rd-progress"><div class="rd-progress-bar" :style="{ width: readPercent + '%' }"></div></div>

    <!-- Mobile overlay -->
    <div v-if="sidebarOpen && !loading" class="rd-overlay" @click="sidebarOpen = false"></div>

    <!-- Sidebar TOC -->
    <aside class="rd-sidebar" :class="{ open: sidebarOpen }">
      <p class="rd-sidebar-title">Mục lục</p>
      <ul class="rd-toc">
        <li v-for="item in tocItems" :key="item.id">
          <button
            class="rd-toc-item"
            :class="[item.level.toLowerCase(), { active: activeToc === item.id }]"
            @click="scrollToToc(item.id)"
          >{{ item.text }}</button>
        </li>
      </ul>
    </aside>

    <!-- Main content -->
    <main class="rd-main" :class="{ 'sidebar-open': sidebarOpen }">
      <div v-if="loading" class="rd-loading">Đang tải bài viết...</div>
      <div v-else-if="error" class="rd-error">{{ error }}</div>

      <article v-else class="rd-article" ref="articleEl" v-html="html"></article>

      <!-- Prev / Next navigation -->
      <div v-if="!loading && !error" class="rd-nav">
        <button v-if="prevDoc" class="rd-nav-btn rd-nav-prev" @click="goTo(prevDoc)">
          ← {{ prevDoc.title }}
        </button>
        <div v-else></div>
        <button v-if="nextDoc" class="rd-nav-btn rd-nav-next" @click="goTo(nextDoc)">
          {{ nextDoc.title }} →
        </button>
      </div>
    </main>

    <!-- Highlight toolbar -->
    <div
      ref="hlToolbar"
      class="hl-toolbar"
      :class="{ visible: hlToolbarVis }"
      :style="{ top: hlToolbarPos.top + 'px', left: hlToolbarPos.left + 'px' }"
    >
      <button class="hl-swatch y" @mousedown.prevent="pickColor('yellow')" title="Vàng"></button>
      <button class="hl-swatch g" @mousedown.prevent="pickColor('green')" title="Xanh lá"></button>
      <button class="hl-swatch b" @mousedown.prevent="pickColor('blue')" title="Xanh lam"></button>
      <button class="hl-swatch p" @mousedown.prevent="pickColor('pink')" title="Hồng"></button>
      <span class="hl-hint">click mark để xoá</span>
    </div>

    <!-- Notes panel -->
    <div class="rd-notes" :class="{ open: notesOpen }">
      <div class="rd-notes-header">
        <span>📝 Ghi chú</span>
        <button class="rd-notes-close" @click="notesOpen = false">✕</button>
      </div>
      <div class="rd-notes-body">
        <div class="rd-notes-input-wrap">
          <textarea
            v-model="noteInput"
            placeholder="Thêm ghi chú cho bài này..."
            class="rd-notes-textarea"
            rows="3"
            @keydown.ctrl.enter="addNote"
          ></textarea>
          <button class="rd-notes-add" :disabled="noteSaving" @click="addNote">
            {{ noteSaving ? '...' : 'Lưu' }}
          </button>
        </div>

        <div v-if="notes.length === 0" class="rd-notes-empty">Chưa có ghi chú nào.</div>

        <div v-for="n in notes" :key="n.id" class="rd-note-item">
          <div v-if="editingNote === n.id">
            <textarea v-model="editContent" class="rd-notes-textarea" rows="3"></textarea>
            <div class="rd-note-actions">
              <button class="rd-note-save" @click="saveEdit(n.id)">Lưu</button>
              <button class="rd-note-cancel" @click="editingNote = null">Huỷ</button>
            </div>
          </div>
          <template v-else>
            <p class="rd-note-content">{{ n.content }}</p>
            <div class="rd-note-meta">
              <span>{{ new Date(n.updatedAt).toLocaleDateString('vi') }}</span>
              <div class="rd-note-actions">
                <button class="rd-note-edit" @click="startEdit(n)">Sửa</button>
                <button class="rd-note-del" @click="removeNote(n.id)">Xoá</button>
              </div>
            </div>
          </template>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ── Shell ───────────────────────────────────────────────────────────── */
.reader { min-height: 100vh; background: #fff; }

/* ── Header ──────────────────────────────────────────────────────────── */
.rd-header {
  position: fixed; top: 0; left: 0; right: 0; z-index: 100;
  height: 48px; background: #fff; border-bottom: 1px solid #e5e7eb;
  display: flex; align-items: center; gap: .5rem; padding: 0 1rem;
  box-shadow: 0 1px 3px rgba(0,0,0,.06);
}
.rd-breadcrumb { flex: 1; font-size: .8rem; color: #9ca3af; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.rd-link { background: none; border: none; cursor: pointer; color: #6b7280; font-size: .8rem; padding: 0; }
.rd-link:hover { color: #0f6fec; text-decoration: underline; }
.rd-header-actions { display: flex; align-items: center; gap: .5rem; }
.rd-icon-btn {
  background: none; border: none; cursor: pointer;
  font-size: 1.1rem; padding: 5px 7px; border-radius: 6px; color: #6b7280;
  display: flex; align-items: center; gap: 2px; position: relative;
}
.rd-icon-btn:hover { background: #f3f4f6; color: #111827; }
.rd-icon-btn.active { color: #0f6fec; background: #e8f1fd; }
.rd-badge {
  position: absolute; top: 2px; right: 2px;
  background: #0f6fec; color: #fff; font-size: .6rem;
  border-radius: 999px; padding: 0 4px; min-width: 14px; text-align: center; line-height: 14px;
}
.rd-percent { font-size: .78rem; color: #9ca3af; font-family: monospace; min-width: 32px; text-align: right; }

/* ── Progress bar ─────────────────────────────────────────────────────── */
.rd-progress { position: fixed; top: 48px; left: 0; right: 0; z-index: 99; height: 3px; background: #f3f4f6; }
.rd-progress-bar { height: 100%; background: linear-gradient(90deg, #0f6fec, #38bdf8); transition: width .2s; }

/* ── Sidebar ──────────────────────────────────────────────────────────── */
.rd-sidebar {
  position: fixed; top: 51px; bottom: 0; left: 0; z-index: 90;
  width: 260px; background: #f9fafb; border-right: 1px solid #e5e7eb;
  overflow-y: auto; transform: translateX(-260px); transition: transform .2s ease;
  padding: .75rem 0 2rem;
}
.rd-sidebar.open { transform: translateX(0); }
.rd-sidebar-title {
  font-size: .68rem; font-weight: 700; letter-spacing: .07em;
  text-transform: uppercase; color: #9ca3af; padding: .4rem 1rem .3rem;
  font-family: -apple-system, sans-serif;
}
.rd-toc { list-style: none; }
.rd-toc-item {
  display: block; width: 100%; text-align: left;
  background: none; border: none; cursor: pointer;
  padding: .28rem 1rem; font-size: .81rem; color: #374151; line-height: 1.4;
  border-left: 2px solid transparent; transition: all .12s;
  font-family: -apple-system, sans-serif;
}
.rd-toc-item:hover { background: #e5e7eb; }
.rd-toc-item.active { color: #0f6fec; background: #e8f1fd; border-left-color: #0f6fec; }
.rd-toc-item.h3 { padding-left: 1.75rem; font-size: .77rem; color: #6b7280; }
.rd-toc-item.h4 { padding-left: 2.5rem; font-size: .74rem; color: #9ca3af; }

/* ── Main ─────────────────────────────────────────────────────────────── */
.rd-main {
  margin-top: 51px; min-height: calc(100vh - 51px);
  transition: margin-left .2s ease;
}
.rd-main.sidebar-open { margin-left: 260px; }
.rd-loading, .rd-error { text-align: center; padding: 4rem; color: #6b7280; font-family: sans-serif; }

/* ── Article typography ───────────────────────────────────────────────── */
.rd-article {
  max-width: 700px; margin: 0 auto; padding: 2.5rem 1.5rem 4rem;
  font-family: Georgia, 'Times New Roman', serif;
  font-size: 17px; line-height: 1.8; color: #1a1a1a;
}
/* deep styles for v-html content */
.rd-article :deep(h1) { font-size: 1.85rem; line-height: 1.25; margin-bottom: .5rem; }
.rd-article :deep(h2) { font-size: 1.3rem; margin: 2.5rem 0 .75rem; padding-top: .25rem; }
.rd-article :deep(h3) { font-size: 1.1rem; margin: 2rem 0 .5rem; }
.rd-article :deep(h4) { font-size: 1rem; margin: 1.5rem 0 .4rem; }
.rd-article :deep(p)  { margin: 1rem 0; }
.rd-article :deep(ul), .rd-article :deep(ol) { margin: 1rem 0 1rem 1.5rem; }
.rd-article :deep(li) { margin: .3rem 0; }
.rd-article :deep(a)  { color: #0f6fec; text-decoration: none; }
.rd-article :deep(a:hover) { text-decoration: underline; }
.rd-article :deep(hr) { border: none; border-top: 1px solid #e5e7eb; margin: 2rem 0; }
.rd-article :deep(blockquote) {
  border-left: 3px solid #0f6fec; padding: .75rem 1rem;
  color: #444; background: #e8f1fd; border-radius: 0 6px 6px 0; margin: 1.25rem 0;
}
.rd-article :deep(code) {
  font-family: 'JetBrains Mono', 'Fira Code', monospace; font-size: .86em;
  background: #f3f4f6; padding: .15em .4em; border-radius: 3px;
}
.rd-article :deep(pre) {
  font-family: 'JetBrains Mono', 'Fira Code', monospace; font-size: .84em;
  background: #1e1e2e; color: #cdd6f4;
  padding: 1.25rem 1.5rem; border-radius: 8px; overflow-x: auto; margin: 1.5rem 0; line-height: 1.6;
}
.rd-article :deep(pre code) { background: none; padding: 0; color: inherit; font-size: 1em; }
.rd-article :deep(img) { max-width: 100%; height: auto; border-radius: 6px; margin: 1.25rem 0; display: block; }
.rd-article :deep(table) { border-collapse: collapse; width: 100%; margin: 1.25rem 0; font-size: .88rem; font-family: sans-serif; }
.rd-article :deep(td), .rd-article :deep(th) { border: 1px solid #e5e7eb; padding: .5rem .75rem; }
.rd-article :deep(th) { background: #f9fafb; font-weight: 600; }

/* highlights restored from DB */
.rd-article :deep(mark.hl) { border-radius: 2px; padding: 1px 0; cursor: pointer; }
.rd-article :deep(mark.hl:hover) { filter: brightness(.88); }
.rd-article :deep(mark.hl-yellow) { background: #fff176; }
.rd-article :deep(mark.hl-green)  { background: #b9f6ca; }
.rd-article :deep(mark.hl-blue)   { background: #bbdefb; }
.rd-article :deep(mark.hl-pink)   { background: #f8bbd0; }

/* ── Prev/Next nav ──────────────────────────────────────────────────── */
.rd-nav {
  max-width: 700px; margin: 0 auto; padding: 1.5rem;
  display: flex; justify-content: space-between; gap: 1rem;
  border-top: 1px solid #e5e7eb;
}
.rd-nav-btn {
  background: #f9fafb; border: 1px solid #e5e7eb; border-radius: 8px;
  padding: .6rem 1rem; cursor: pointer; font-size: .82rem; color: #374151;
  font-family: -apple-system, sans-serif; transition: all .12s; max-width: 48%;
  white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
}
.rd-nav-btn:hover { background: #e8f1fd; border-color: #0f6fec; color: #0f6fec; }
.rd-nav-next { margin-left: auto; }

/* ── Highlight toolbar ───────────────────────────────────────────────── */
.hl-toolbar {
  position: absolute; z-index: 200;
  background: #1f2937; border-radius: 8px;
  padding: 6px 10px; display: none; gap: 6px; align-items: center;
  box-shadow: 0 4px 16px rgba(0,0,0,.3);
}
.hl-toolbar.visible { display: flex; }
.hl-swatch {
  width: 22px; height: 22px; border-radius: 50%;
  border: 2px solid rgba(255,255,255,.25); cursor: pointer; transition: transform .1s;
}
.hl-swatch:hover { transform: scale(1.25); }
.hl-swatch.y { background: #fff176; }
.hl-swatch.g { background: #b9f6ca; }
.hl-swatch.b { background: #bbdefb; }
.hl-swatch.p { background: #f8bbd0; }
.hl-hint { font-size: .68rem; color: #6b7280; margin-left: 4px; white-space: nowrap; }

/* ── Notes panel ─────────────────────────────────────────────────────── */
.rd-notes {
  position: fixed; bottom: 0; right: 0; z-index: 150;
  width: 320px; max-height: 70vh;
  background: #fff; border: 1px solid #e5e7eb;
  border-radius: 10px 10px 0 0;
  box-shadow: 0 -4px 20px rgba(0,0,0,.1);
  display: flex; flex-direction: column;
  transform: translateY(100%); transition: transform .25s ease;
}
.rd-notes.open { transform: translateY(0); }
.rd-notes-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: .6rem 1rem; border-bottom: 1px solid #e5e7eb;
  font-size: .85rem; font-weight: 600; font-family: sans-serif;
}
.rd-notes-close {
  background: none; border: none; cursor: pointer; color: #9ca3af; font-size: 1rem; padding: 2px 6px;
}
.rd-notes-close:hover { color: #111827; }
.rd-notes-body { flex: 1; overflow-y: auto; padding: .75rem; display: flex; flex-direction: column; gap: .6rem; }
.rd-notes-input-wrap { display: flex; flex-direction: column; gap: .4rem; }
.rd-notes-textarea {
  width: 100%; border: 1px solid #e5e7eb; border-radius: 6px;
  padding: .6rem .75rem; font-family: sans-serif; font-size: .83rem;
  resize: none; outline: none; transition: border-color .15s;
}
.rd-notes-textarea:focus { border-color: #0f6fec; }
.rd-notes-add {
  align-self: flex-end; background: #0f6fec; color: #fff; border: none;
  border-radius: 6px; padding: .35rem .9rem; font-size: .8rem;
  font-family: sans-serif; cursor: pointer; font-weight: 600;
}
.rd-notes-add:disabled { opacity: .6; cursor: default; }
.rd-notes-empty { text-align: center; color: #9ca3af; font-size: .82rem; font-family: sans-serif; padding: 1rem; }
.rd-note-item {
  background: #fffbeb; border: 1px solid #fde68a; border-radius: 6px; padding: .6rem .75rem;
}
.rd-note-content { font-size: .83rem; color: #1a1a1a; font-family: sans-serif; white-space: pre-wrap; }
.rd-note-meta {
  display: flex; align-items: center; justify-content: space-between; margin-top: .4rem;
  font-size: .72rem; color: #9ca3af; font-family: sans-serif;
}
.rd-note-actions { display: flex; gap: .4rem; }
.rd-note-edit, .rd-note-del, .rd-note-save, .rd-note-cancel {
  background: none; border: none; cursor: pointer; font-size: .75rem; padding: 2px 6px; border-radius: 4px;
}
.rd-note-edit:hover { color: #0f6fec; background: #e8f1fd; }
.rd-note-del:hover { color: #ef4444; background: #fee2e2; }
.rd-note-save { color: #0f6fec; font-weight: 600; }
.rd-note-cancel { color: #6b7280; }

/* ── Overlay (mobile) ─────────────────────────────────────────────────── */
.rd-overlay {
  display: none; position: fixed; inset: 0; z-index: 85; background: rgba(0,0,0,.35);
}

/* ── Mobile ────────────────────────────────────────────────────────────── */
@media (max-width: 768px) {
  .rd-sidebar { width: 100vw; }
  .rd-main.sidebar-open { margin-left: 0; }
  .rd-overlay { display: block; }
  .rd-article { padding: 1.5rem 1rem 4rem; font-size: 16px; }
  .rd-article :deep(h1) { font-size: 1.5rem; }
  .rd-notes { width: 100vw; }
}
</style>
