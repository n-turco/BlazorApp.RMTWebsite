document.addEventListener('submit', (event) => {
    const button = event.target.querySelector('button[type="submit"][data-disable-on-submit]');
    if (!button) {
        return;
    }

    // Wait one tick so Blazor reads the form before the button is disabled.
    setTimeout(() => {
        button.disabled = true;
        button.textContent = 'Sending…';
    }, 0);
});
