<script setup>
import { computed, ref } from 'vue'
import ContextMenu from 'primevue/contextmenu'
import { linkify } from '../utils/linkify'

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
  }
  return items
})

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
</style>
