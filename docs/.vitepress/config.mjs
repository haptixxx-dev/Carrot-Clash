import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'Carrot Clash',
  description: 'Game Design & Engineering Documentation',
  base: '/',
  themeConfig: {
    nav: [
      { text: 'Home', link: '/' },
      { text: 'GDD', link: '/gdd' },
      { text: 'Developers', link: '/dev/getting-started' },
      { text: 'Milestones', link: '/milestones' },
    ],
    sidebar: [
      {
        text: 'Overview',
        items: [
          { text: 'Index', link: '/' },
          { text: 'GDD (Master)', link: '/gdd' },
          { text: 'Overview', link: '/overview' },
          { text: 'Milestones', link: '/milestones' },
        ],
      },
      {
        text: 'For Developers',
        items: [
          { text: 'Getting Started', link: '/dev/getting-started' },
          { text: 'Environment Setup', link: '/dev/environment' },
          { text: 'Code Architecture', link: '/dev/architecture' },
          { text: 'Editor Setup & Wiring', link: '/dev/editor-setup' },
          { text: 'Asset Checklist', link: '/dev/assets' },
          { text: 'Implementation Status', link: '/dev/status' },
        ],
      },
      {
        text: 'Gameplay',
        items: [
          { text: 'Core Loop', link: '/core-loop' },
          { text: 'Momentum System', link: '/momentum-system' },
          { text: 'Characters', link: '/characters' },
          { text: 'Weapons', link: '/weapons' },
          { text: 'Ability Interactions', link: '/ability-interactions' },
        ],
      },
      {
        text: 'World & Feel',
        items: [
          { text: 'Map', link: '/map' },
          { text: 'Game Feel', link: '/game-feel' },
          { text: 'Audio', link: '/audio' },
        ],
      },
      {
        text: 'Player Systems',
        items: [
          { text: 'Progression', link: '/progression' },
          { text: 'UI / UX', link: '/ui-ux' },
        ],
      },
      {
        text: 'Technical',
        items: [
          { text: 'Tech Architecture', link: '/tech-architecture' },
        ],
      },
    ],
    search: { provider: 'local' },
    editLink: {
      pattern: 'https://github.com/haptixxx-dev/Carrot-Clash/edit/release/docs/:path',
      text: 'Edit this page',
    },
    footer: {
      message: 'Carrot Clash — Internal Design & Engineering Documentation',
    },
  },
})
