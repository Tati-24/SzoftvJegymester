<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import {
    createFilm,
    deleteFilm,
    getFilms,
    updateFilm,
    type Film
  } from './lib/api';
  import './styles/FilmEdit.css';

  export let isLoggedIn = false;
  export let isAdmin = false;

  const dispatch = createEventDispatcher<{
    goLogin: void;
    goRegister: void;
    goHome: void;
    goProfile: void;
    goCart: void;
  }>();

  type FilmForm = {
    id: string;
    title: string;
    description: string;
    length: number;
    ageRating: string;
    releaseDate: string;
    genre: string;
    director: string;
    isActive: boolean;
  };

  let films: Film[] = [];
  let isLoading = true;
  let saveLoading = false;
  let deleteLoading = false;
  let error = '';
  let info = '';
  let isNewMode = false;

  let form: FilmForm = emptyForm();

  function emptyForm(): FilmForm {
    return {
      id: '',
      title: '',
      description: '',
      length: 90,
      ageRating: '12',
      releaseDate: new Date().toISOString().slice(0, 10),
      genre: '',
      director: '',
      isActive: true
    };
  }

  function mapFilmToForm(film: Film): FilmForm {
    return {
      id: film.id,
      title: film.title,
      description: film.description,
      length: film.length,
      ageRating: film.ageRating ?? '',
      releaseDate: film.releaseDate.slice(0, 10),
      genre: film.genre ?? '',
      director: film.director ?? '',
      isActive: film.isActive
    };
  }

  function releaseDateForApi(date: string): string {
    return `${date}T00:00:00`;
  }

  async function loadFilms() {
    isLoading = true;
    error = '';

    try {
      films = await getFilms();
      if (films.length === 0) {
        isNewMode = true;
        form = emptyForm();
      } else if (!isNewMode && form.id && films.some((f) => f.id === form.id)) {
        const current = films.find((f) => f.id === form.id)!;
        form = mapFilmToForm(current);
      } else if (!isNewMode && films.length > 0) {
        form = mapFilmToForm(films[0]);
      }
    } catch (e) {
      error = e instanceof Error ? e.message : 'Nem sikerült betölteni a filmeket.';
    } finally {
      isLoading = false;
    }
  }

  function selectFilm(id: string) {
    isNewMode = false;
    const film = films.find((item) => item.id === id);
    if (!film) return;
    form = mapFilmToForm(film);
    info = '';
    error = '';
  }

  function startNewFilm() {
    isNewMode = true;
    form = emptyForm();
    info = '';
    error = '';
  }

  function validateForm(): string | null {
    const title = form.title.trim();
    if (!title) return 'A cím megadása kötelező.';
    const desc = form.description.trim();
    if (!desc) return 'A leírás megadása kötelező.';
    if (desc.length > 4000) return 'A leírás legfeljebb 4000 karakter lehet.';
    if (!Number.isInteger(form.length) || form.length < 1 || form.length > 1000) {
      return 'A játékidő 1 és 1000 perc közötti egész szám legyen.';
    }
    if (!form.releaseDate) return 'A megjelenés dátuma kötelező.';
    return null;
  }

  async function handleSave() {
    info = '';
    error = '';
    const validation = validateForm();
    if (validation) {
      error = validation;
      return;
    }

    const payloadCommon = {
      title: form.title.trim(),
      description: form.description.trim(),
      length: form.length,
      ageRating: form.ageRating.trim() || null,
      releaseDate: releaseDateForApi(form.releaseDate),
      genre: form.genre.trim() || null,
      director: form.director.trim() || null,
      isActive: form.isActive
    };

    saveLoading = true;
    try {
      if (isNewMode) {
        const created = await createFilm(payloadCommon);
        info = 'Film sikeresen létrehozva.';
        isNewMode = false;
        await loadFilms();
        form = mapFilmToForm(created);
      } else {
        const updated = await updateFilm(form.id, payloadCommon);
        info = 'Módosítások elmentve.';
        await loadFilms();
        form = mapFilmToForm(updated);
      }
    } catch (e) {
      error = e instanceof Error ? e.message : 'A mentés nem sikerült.';
    } finally {
      saveLoading = false;
    }
  }

  async function handleDelete() {
    if (isNewMode || !form.id) return;
    if (!confirm('Biztosan törlöd ezt a filmet? Ez nem vonható vissza.')) return;

    info = '';
    error = '';
    deleteLoading = true;
    try {
      await deleteFilm(form.id);
      info = 'Film törölve.';
      await loadFilms();
      if (films.length > 0) {
        form = mapFilmToForm(films[0]);
        isNewMode = false;
      } else {
        startNewFilm();
      }
    } catch (e) {
      error = e instanceof Error ? e.message : 'A törlés nem sikerült.';
    } finally {
      deleteLoading = false;
    }
  }

  function onPickerChange(e: Event) {
    const value = (e.currentTarget as HTMLSelectElement).value;
    if (value === '__new__') {
      startNewFilm();
    } else {
      selectFilm(value);
    }
  }

  onMount(() => {
    loadFilms();
  });

</script>

<div class="film-edit-page">
  <nav class="navbar">
    <button type="button" class="navbar-brand navbar-brand-link" on:click={() => dispatch('goHome')}>Jegymester</button>
    <div class="navbar-menu">
      {#if isAdmin}
        <button type="button" class="navbar-link active">Admin felület</button>
      {/if}
      {#if isLoggedIn}
        <button type="button" class="navbar-link" on:click={() => dispatch('goProfile')}>Profil</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goCart')}>Kosár</button>
      {:else}
        <button type="button" class="navbar-link" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goRegister')}>Regisztráció</button>
      {/if}
    </div>
  </nav>

  <main class="film-edit-layout">
    {#if !isLoggedIn || !isAdmin}
      <section class="film-form card">
        <h2>Nincs jogosultság</h2>
        <p class="muted">A filmek módosítása csak admin felhasználóknak érhető el.</p>
        <button type="button" class="save-btn" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
      </section>
    {:else}
      <section class="film-picker card admin-tools">
        <h2>Film kiválasztása</h2>
        {#if isLoading}
          <p class="muted">Filmek betöltése...</p>
        {:else}
          <select
            value={isNewMode ? '__new__' : form.id}
            on:change={onPickerChange}
            disabled={saveLoading || deleteLoading}
          >
            <option value="__new__">Új film…</option>
            {#each films as film}
              <option value={film.id}>{film.title}</option>
            {/each}
          </select>
          <button type="button" class="picker-new-btn" on:click={startNewFilm} disabled={saveLoading || deleteLoading}>
            Új film űrlap
          </button>
        {/if}

      </section>

      <section class="film-form card">
        <h2>{isNewMode ? 'Új film' : 'Film adatok módosítása'}</h2>
        <form on:submit|preventDefault={handleSave}>
          <label>
            Cím
            <input type="text" bind:value={form.title} required disabled={saveLoading || deleteLoading} />
          </label>
          <label>
            Leírás
            <textarea rows="4" bind:value={form.description} required disabled={saveLoading || deleteLoading}></textarea>
          </label>
          <div class="grid">
            <label>
              Játékidő (perc)
              <input type="number" min="1" max="1000" step="1" bind:value={form.length} required disabled={saveLoading || deleteLoading} />
            </label>
            <label>
              Korhatár
              <input type="text" bind:value={form.ageRating} disabled={saveLoading || deleteLoading} />
            </label>
            <label>
              Megjelenés
              <input type="date" bind:value={form.releaseDate} required disabled={saveLoading || deleteLoading} />
            </label>
            <label>
              Műfaj
              <input type="text" bind:value={form.genre} disabled={saveLoading || deleteLoading} />
            </label>
            <label>
              Rendező
              <input type="text" bind:value={form.director} disabled={saveLoading || deleteLoading} />
            </label>
          </div>

          <label class="checkbox">
            <input type="checkbox" bind:checked={form.isActive} disabled={saveLoading || deleteLoading} />
            Aktív film
          </label>

          <div class="form-actions">
            <button type="submit" class="save-btn" disabled={saveLoading || deleteLoading}>
              {#if saveLoading}
                Mentés…
              {:else if isNewMode}
                Film létrehozása
              {:else}
                Módosítás mentése
              {/if}
            </button>
            <button
              type="button"
              class="delete-btn"
              on:click={handleDelete}
              disabled={saveLoading || deleteLoading || isNewMode || !form.id}
            >
              {#if deleteLoading}
                Törlés…
              {:else}
                Film törlése
              {/if}
            </button>
          </div>
        </form>

        {#if info}
          <p class="ok">{info}</p>
        {/if}
        {#if error}
          <p class="error">{error}</p>
        {/if}
      </section>
    {/if}
  </main>
</div>
