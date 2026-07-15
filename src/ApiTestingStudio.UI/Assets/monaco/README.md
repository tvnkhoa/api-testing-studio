# Monaco editor assets (offline)

The API Runner hosts the request/response editors with [Monaco](https://microsoft.github.io/monaco-editor/)
inside a WebView2, loaded **fully offline** via `CoreWebView2.SetVirtualHostNameToFolderMapping`
(no CDN — see the offline mandate in `.claude/CLAUDE.md`).

## What ships in the repo
- `editor.html` — the loader/host page. It exposes `window.editorApi.apply(text, language, readOnly)`
  and posts edits back to WPF. **It self-degrades**: if the `vs/` bundle is missing it mounts a plain
  `<textarea>`, so the Runner stays usable even without Monaco.
- `vs/` — the Monaco distribution (`monaco-editor@0.55.1`, `min/vs`), committed so the app runs
  **fully offline** with no build-time network access.

## Refreshing / re-pinning the Monaco bundle
`vs/` is the vendored `min/vs` folder of the `monaco-editor` npm package. To update the pinned
version:

```
# on a machine with network access:
npm pack monaco-editor@<version>
tar -xzf monaco-editor-*.tgz
rm -rf src/ApiTestingStudio.UI/Assets/monaco/vs
cp -r package/min/vs src/ApiTestingStudio.UI/Assets/monaco/vs   # must yield vs/loader.js
```

The build copies everything under `Assets/**` to the output directory. See
`.claude/DECISIONS/ADR-0009-*` for the rationale.
