import { createRouter, createWebHistory } from 'vue-router'
import LoginView from '../views/LoginView.vue'
import RegisterView from '../views/RegisterView.vue'
import TeamView from '../views/TeamView.vue'
import BoardView from '../views/BoardView.vue'

const routes = [
  { path: '/login', component: LoginView, meta: { public: true } },
  { path: '/register', component: RegisterView, meta: { public: true } },
  { path: '/teams', component: TeamView },
  { path: '/board/:teamId', component: BoardView },
  { path: '/', redirect: '/teams' },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach((to) => {
  const token = localStorage.getItem('token')
  if (!to.meta.public && !token) return '/login'
  if (to.meta.public && token && (to.path === '/login' || to.path === '/register')) return '/teams'
})

export default router
