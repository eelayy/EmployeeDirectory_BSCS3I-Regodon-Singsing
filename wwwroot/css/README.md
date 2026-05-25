Design CSS

This folder contains the project's CSS design assets.

- `design.css`: A lightweight theme file with CSS variables and overrides to give the app a modern, clean look while keeping Bootstrap as the base.

Usage

- Leave the Bootstrap CDN/local link in place, then include `design.css` after `site.css` so design overrides can apply.
- To enable the theme on pages, add `class="app-theme"` to the `<body>` tag in `Views/Shared/_Layout.cshtml`.

Want more?

- I can split this into `base/`, `components/`, and `pages/` files or add Sass + npm build tooling if you want a scalable pipeline.
