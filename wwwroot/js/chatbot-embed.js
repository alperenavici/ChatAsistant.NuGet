/**
 * ChatAsistant Embeddable Widget
 *
 * Usage (in any project that has ChatAsistant NuGet package):
 *   <script src="/_chatasistant/js/chatbot-embed.js"></script>
 *
 * Optional attributes:
 *   data-api-url  - Override API base URL (default: same origin)
 *   data-prefix   - Override route prefix (default: _chatasistant)
 *   data-api-key  - API key for authenticated endpoints
 */
(() => {
  const scriptEl = document.currentScript;
  const API_URL = (scriptEl.getAttribute("data-api-url") || "").replace(/\/+$/, "");
  const PREFIX = scriptEl.getAttribute("data-prefix") || "_chatasistant";
  const API_KEY = scriptEl.getAttribute("data-api-key") || "";

  const BASE = API_URL ? API_URL + "/" + PREFIX : "/" + PREFIX;

  const STYLES = `
    .cb-embed-container {
      position: fixed;
      right: 24px;
      bottom: 24px;
      z-index: 999999;
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 12px;
      font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
    }
    .cb-embed-window {
      width: 360px;
      max-height: 520px;
      background: #fff;
      border-radius: 20px;
      box-shadow: 0 18px 45px rgba(15,23,42,.25);
      overflow: hidden;
      display: none;
      flex-direction: column;
      transform-origin: bottom right;
      transform: scale(.9);
      opacity: 0;
      transition: opacity .2s ease-out, transform .2s ease-out;
    }
    .cb-embed-window.cb-open {
      display: flex;
      opacity: 1;
      transform: scale(1);
    }
    .cb-embed-header {
      padding: 14px 16px;
      background: linear-gradient(135deg, #2563eb, #4f46e5);
      color: #fff;
      display: flex;
      align-items: center;
      justify-content: space-between;
    }
    .cb-embed-header-left {
      display: flex;
      align-items: center;
      gap: 10px;
    }
    .cb-embed-header-avatar {
      width: 32px; height: 32px;
      border-radius: 50%;
      background: #fff;
      display: flex; align-items: center; justify-content: center;
      overflow: hidden;
    }
    .cb-embed-header-avatar img {
      width: 28px; height: 28px; object-fit: contain;
    }
    .cb-embed-header-title {
      display: flex; flex-direction: column;
    }
    .cb-embed-header-title span:first-child {
      font-weight: 600; font-size: 14px;
    }
    .cb-embed-header-title span:last-child {
      font-size: 11px; opacity: .9;
    }
    .cb-embed-close {
      background: transparent; border: none; color: #e5e7eb;
      cursor: pointer; font-size: 18px; padding: 4px; border-radius: 999px;
      transition: background .15s, color .15s;
    }
    .cb-embed-close:hover { background: rgba(15,23,42,.18); color: #fff; }
    .cb-embed-messages {
      padding: 12px 14px 8px;
      background: #f9fafb;
      flex: 1;
      overflow-y: auto;
      scrollbar-width: thin;
      scrollbar-color: #cbd5f5 transparent;
    }
    .cb-embed-messages::-webkit-scrollbar { width: 6px; }
    .cb-embed-messages::-webkit-scrollbar-thumb { background: #cbd5f5; border-radius: 999px; }
    .cb-embed-msg { display: flex; margin-bottom: 8px; }
    .cb-embed-msg.cb-bot { justify-content: flex-start; }
    .cb-embed-msg.cb-user { justify-content: flex-end; }
    .cb-embed-bubble {
      max-width: 80%; border-radius: 16px; padding: 8px 12px;
      font-size: 13px; line-height: 1.4;
      display: flex; flex-direction: column; align-items: flex-start; gap: 10px;
    }
    .cb-embed-msg.cb-bot .cb-embed-bubble {
      background: #e5edff; color: #111827; border-bottom-left-radius: 4px;
    }
    .cb-embed-msg.cb-user .cb-embed-bubble {
      background: #2563eb; color: #fff; border-bottom-right-radius: 4px;
    }
    .cb-embed-typing {
      display: none; font-size: 11px; color: #6b7280; padding: 0 4px 4px 4px;
    }
    .cb-embed-typing.cb-visible { display: block; }
    .cb-embed-typing-dots { display: inline-flex; gap: 3px; }
    .cb-embed-typing-dots span {
      width: 4px; height: 4px; border-radius: 50%; background: #9ca3af;
      animation: cbTyping 1s infinite ease-in-out;
    }
    .cb-embed-typing-dots span:nth-child(2) { animation-delay: .15s; }
    .cb-embed-typing-dots span:nth-child(3) { animation-delay: .3s; }
    @keyframes cbTyping {
      0%,80%,100% { transform: translateY(0); opacity: .3; }
      40% { transform: translateY(-3px); opacity: 1; }
    }
    .cb-embed-input-area {
      padding: 8px 10px 10px; border-top: 1px solid #e5e7eb;
      background: #fff; display: flex; align-items: center; gap: 8px;
    }
    .cb-embed-input {
      flex: 1; border-radius: 999px; border: 1px solid #e5e7eb;
      padding: 8px 12px; font-size: 13px; outline: none;
      transition: border-color .15s, box-shadow .15s;
      font-family: inherit;
    }
    .cb-embed-input:focus {
      border-color: #2563eb; box-shadow: 0 0 0 1px rgba(37,99,235,.35);
    }
    .cb-embed-send {
      border-radius: 999px; border: none; padding: 8px 14px;
      font-size: 13px; font-weight: 500; cursor: pointer;
      background: #2563eb; color: #fff;
      display: inline-flex; align-items: center; gap: 4px;
      transition: background .15s, transform .1s, box-shadow .1s;
      font-family: inherit;
    }
    .cb-embed-send:hover { background: #1d4ed8; box-shadow: 0 6px 14px rgba(37,99,235,.32); }
    .cb-embed-send:active { transform: translateY(1px); box-shadow: none; }
    .cb-embed-send[disabled] { opacity: .5; cursor: default; box-shadow: none; }
    .cb-embed-fab {
      width: 72px; height: 72px; border-radius: 50%; background: #fff;
      border: none; box-shadow: 0 18px 40px rgba(15,23,42,.28);
      display: flex; align-items: center; justify-content: center;
      cursor: pointer; position: relative; overflow: hidden;
      transition: transform .18s, box-shadow .18s;
    }
    .cb-embed-fab img {
      width: 64px; height: 64px; object-fit: contain;
    }
    .cb-embed-fab:hover { transform: translateY(-2px) scale(1.03); box-shadow: 0 20px 46px rgba(15,23,42,.32); }
    .cb-embed-fab:active { transform: translateY(1px) scale(.97); }
    .cb-embed-link-chip {
      align-self: flex-end; display: inline-flex; align-items: center; gap: 6px;
      margin-top: 4px; padding: 5px 10px; font-size: 11px; font-weight: 500;
      color: #4b5563; background: #fff; border: 1px solid #e5e7eb;
      border-radius: 999px; cursor: pointer;
      transition: background .15s, border-color .15s, transform .1s, box-shadow .1s;
      font-family: inherit;
    }
    .cb-embed-link-chip:hover { background: #f3f4f6; border-color: #d1d5db; box-shadow: 0 4px 10px rgba(15,23,42,.08); }
    .cb-embed-link-chip:active { transform: scale(.98); }
    .cb-embed-error { color: #b91c1c; }
    @media (max-width: 640px) {
      .cb-embed-window { width: 100%; max-width: 100%; border-radius: 18px 18px 0 0; }
      .cb-embed-container { right: 12px; bottom: 12px; }
      .cb-embed-fab { width: 56px; height: 56px; font-size: 24px; }
    }
  `;

  const BOT_AVATAR_URL = BASE + "/images/bot-avatar.png";
  const LINK_ARROW_SVG = '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M7 17L17 7M17 7h-8M17 7v8"/></svg>';

  const style = document.createElement("style");
  style.textContent = STYLES;
  document.head.appendChild(style);

  const container = document.createElement("div");
  container.className = "cb-embed-container";
  container.innerHTML = `
    <div class="cb-embed-window">
      <header class="cb-embed-header">
        <div class="cb-embed-header-left">
          <div class="cb-embed-header-avatar"><img src="${BOT_AVATAR_URL}" alt="Asistan" /></div>
          <div class="cb-embed-header-title">
            <span>Yardımcı Asistan</span>
            <span>Platform ile ilgili sorularını sor</span>
          </div>
        </div>
        <button class="cb-embed-close" aria-label="Kapat">&times;</button>
      </header>
      <section class="cb-embed-messages" aria-live="polite"></section>
      <div class="cb-embed-typing">
        <span>Asistan yazıyor</span>
        <span class="cb-embed-typing-dots"><span></span><span></span><span></span></span>
      </div>
      <footer class="cb-embed-input-area">
        <input class="cb-embed-input" type="text" placeholder="Mesajını yaz ve Enter'a bas..." autocomplete="off" />
        <button class="cb-embed-send" type="button">Gönder</button>
      </footer>
    </div>
    <button class="cb-embed-fab" type="button" aria-label="Yardım asistanını aç"><img src="${BOT_AVATAR_URL}" alt="Yardım asistanı" /></button>
  `;
  document.body.appendChild(container);

  const windowEl = container.querySelector(".cb-embed-window");
  const messagesEl = container.querySelector(".cb-embed-messages");
  const typingEl = container.querySelector(".cb-embed-typing");
  const inputEl = container.querySelector(".cb-embed-input");
  const sendBtn = container.querySelector(".cb-embed-send");
  const closeBtn = container.querySelector(".cb-embed-close");
  const fab = container.querySelector(".cb-embed-fab");

  let isOpen = false;
  let isSending = false;

  function toggle(forceOpen) {
    const shouldOpen = typeof forceOpen === "boolean" ? forceOpen : !isOpen;
    isOpen = shouldOpen;
    if (shouldOpen) {
      windowEl.classList.add("cb-open");
      setTimeout(() => inputEl.focus(), 120);
    } else {
      windowEl.classList.remove("cb-open");
    }
  }

  function appendMessage(text, from, options) {
    options = options || {};
    const wrapper = document.createElement("div");
    wrapper.className = "cb-embed-msg " + (from === "user" ? "cb-user" : "cb-bot");

    const bubble = document.createElement("div");
    bubble.className = "cb-embed-bubble";
    if (options.error) bubble.classList.add("cb-embed-error");
    bubble.textContent = text;

    if (options.linkUrl) {
      const chip = document.createElement("button");
      chip.type = "button";
      chip.className = "cb-embed-link-chip";
      chip.innerHTML = "<span>Sayfayı aç</span>" + LINK_ARROW_SVG;
      chip.addEventListener("click", () => { window.location.href = options.linkUrl; });
      bubble.appendChild(chip);
    }

    wrapper.appendChild(bubble);
    messagesEl.appendChild(wrapper);
    messagesEl.scrollTop = messagesEl.scrollHeight;
  }

  function setTyping(v) {
    typingEl.classList.toggle("cb-visible", v);
  }

  async function sendMessage() {
    const raw = inputEl.value.trim();
    if (!raw || isSending) return;

    appendMessage(raw, "user");
    inputEl.value = "";
    isSending = true;
    sendBtn.disabled = true;
    setTyping(true);

    try {
      const headers = { "Content-Type": "application/json" };
      if (API_KEY) headers["X-API-Key"] = API_KEY;

      const res = await fetch(BASE + "/api/chat", {
        method: "POST",
        headers,
        body: JSON.stringify({ message: raw })
      });

      if (!res.ok) throw new Error("HTTP " + res.status);

      const data = await res.json();
      const text = data.mesaj || data.Mesaj || data.message || "Şu anda cevap veremiyorum.";
      const targetUrl = data.yonlendirilecekUrl || data.YonlendirilecekUrl || null;

      appendMessage(text, "bot", { linkUrl: targetUrl || undefined });
    } catch (err) {
      console.error("[ChatAsistant]", err);
      appendMessage("Bir hata oluştu, lütfen daha sonra tekrar dene.", "bot", { error: true });
    } finally {
      isSending = false;
      sendBtn.disabled = false;
      setTyping(false);
    }
  }

  fab.addEventListener("click", () => toggle());
  closeBtn.addEventListener("click", () => toggle(false));
  sendBtn.addEventListener("click", sendMessage);
  inputEl.addEventListener("keydown", (e) => {
    if (e.key === "Enter" && !e.shiftKey) { e.preventDefault(); sendMessage(); }
  });

  appendMessage("Merhaba, sana platformla ilgili yardımcı olabilirim. Nasıl yardımcı olabilirim?", "bot");
})();
