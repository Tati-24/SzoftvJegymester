<script lang="ts">
  import { createEventDispatcher } from 'svelte';

  export let open = false;
  export let message = '';
  export let title = 'Megerősítés';
  export let confirmLabel = 'Törlés';
  export let cancelLabel = 'Mégse';
  export let busy = false;

  const dispatch = createEventDispatcher<{ confirm: void; cancel: void }>();

  function onBackdropMouseDown(e: MouseEvent) {
    if (busy) return;
    if (e.target === e.currentTarget) dispatch('cancel');
  }
</script>

{#if open}
  <div
    class="confirm-overlay"
    role="presentation"
    on:mousedown={onBackdropMouseDown}
  >
    <div
      class="confirm-panel"
      role="alertdialog"
      aria-modal="true"
      tabindex="-1"
      aria-labelledby="confirm-dialog-title"
      aria-describedby="confirm-dialog-desc"
      on:mousedown|stopPropagation
    >
      <h2 id="confirm-dialog-title" class="confirm-title">{title}</h2>
      <p id="confirm-dialog-desc" class="confirm-message">{message}</p>
      <div class="confirm-actions">
        <button type="button" class="confirm-cancel" disabled={busy} on:click={() => dispatch('cancel')}>
          {cancelLabel}
        </button>
        <button type="button" class="confirm-ok delete-btn" disabled={busy} on:click={() => dispatch('confirm')}>
          {#if busy}
            Feldolgozás…
          {:else}
            {confirmLabel}
          {/if}
        </button>
      </div>
    </div>
  </div>
{/if}

<style>
  .confirm-overlay {
    position: fixed;
    inset: 0;
    z-index: 2000;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 1rem;
    background: rgba(0, 0, 0, 0.55);
    box-sizing: border-box;
  }

  .confirm-panel {
    width: 100%;
    max-width: 420px;
    padding: 1.35rem 1.5rem;
    border-radius: 12px;
    background: var(--card);
    color: var(--text);
    box-shadow: 0 12px 40px rgba(0, 0, 0, 0.55);
    border: 1px solid var(--border);
  }

  .confirm-title {
    margin: 0 0 0.65rem;
    font-size: 1.1rem;
  }

  .confirm-message {
    margin: 0 0 1.25rem;
    font-size: 0.95rem;
    line-height: 1.45;
  }

  .confirm-actions {
    display: flex;
    flex-wrap: wrap;
    gap: 0.65rem;
    justify-content: flex-end;
  }

  .confirm-cancel {
    border: 1px solid var(--border);
    border-radius: 8px;
    padding: 0.55rem 1rem;
    background: var(--input-bg);
    color: var(--text);
    cursor: pointer;
    font-family: inherit;
    font-weight: 600;
    font-size: 0.9rem;
  }

  .confirm-cancel:hover:not(:disabled) {
    filter: brightness(1.08);
  }

  .confirm-cancel:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }

  :global(.confirm-ok.delete-btn) {
    width: auto;
  }
</style>
