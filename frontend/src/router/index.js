import { createRouter, createWebHistory } from 'vue-router'
import LoginView from '../views/LoginView.vue'
import RegisterView from '../views/RegisterView.vue'
import HomeView from '../views/HomeView.vue'
import TeamView from '../views/TeamView.vue'
import BoardView from '../views/BoardView.vue'
import LibraryView from '../views/LibraryView.vue'
import SourceView from '../views/SourceView.vue'
import ReaderView from '../views/ReaderView.vue'

const routes = [
  { path: '/login',    component: LoginView,    meta: { public: true } },
  { path: '/register', component: RegisterView, meta: { public: true } },
  { path: '/',         component: HomeView },
  { path: '/teams',    component: TeamView },
  { path: '/board/:teamId', component: BoardView },
  { path: '/library',  component: LibraryView },
  { path: '/library/:slug', component: SourceView },
  { path: '/library/:sourceSlug/:docSlug', component: ReaderView },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach((to) => {
  const token = localStorage.getItem('token')
  if (!to.meta.public && !token) return '/login'
  if (to.meta.public && token && (to.path === '/login' || to.path === '/register')) return '/'
})

export default router
