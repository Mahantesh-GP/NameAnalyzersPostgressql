/**
 * Client-side JavaScript for Phonetic Search UI
 * Handles autocomplete, form enhancements, and UI interactions
 */

// Debounce utility function
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Autocomplete for name fields
document.addEventListener('DOMContentLoaded', function() {
    const firstNameInput = document.getElementById('first_name');
    const lastNameInput = document.getElementById('last_name');
    const countyInput = document.getElementById('county');

    // Setup autocomplete for each field
    if (firstNameInput) {
        setupAutocomplete(firstNameInput, 'first_name');
    }
    if (lastNameInput) {
        setupAutocomplete(lastNameInput, 'last_name');
    }
    if (countyInput) {
        setupAutocomplete(countyInput, 'county');
    }
});

function setupAutocomplete(inputElement, fieldName) {
    let suggestionsDiv = null;

    // Create suggestions dropdown
    function createSuggestionsDiv() {
        const div = document.createElement('div');
        div.className = 'absolute z-10 w-full mt-1 bg-white border border-gray-300 rounded-lg shadow-lg max-h-60 overflow-y-auto hidden';
        div.style.top = inputElement.offsetHeight + 'px';
        inputElement.parentElement.style.position = 'relative';
        inputElement.parentElement.appendChild(div);
        return div;
    }

    if (!suggestionsDiv) {
        suggestionsDiv = createSuggestionsDiv();
    }

    // Fetch and display suggestions
    const fetchSuggestions = debounce(async function(value) {
        if (value.length < 2) {
            suggestionsDiv.classList.add('hidden');
            return;
        }

        try {
            const response = await fetch(
                `/api/suggestions?field=${fieldName}&prefix=${encodeURIComponent(value)}&limit=10`
            );
            
            if (!response.ok) {
                throw new Error('Failed to fetch suggestions');
            }

            const suggestions = await response.json();
            
            if (suggestions.length === 0) {
                suggestionsDiv.classList.add('hidden');
                return;
            }

            // Render suggestions
            suggestionsDiv.innerHTML = suggestions.map(suggestion => `
                <div class="px-4 py-2 hover:bg-indigo-50 cursor-pointer transition" 
                     data-value="${suggestion}">
                    ${suggestion}
                </div>
            `).join('');

            // Add click handlers
            suggestionsDiv.querySelectorAll('[data-value]').forEach(item => {
                item.addEventListener('click', function() {
                    inputElement.value = this.dataset.value;
                    suggestionsDiv.classList.add('hidden');
                });
            });

            suggestionsDiv.classList.remove('hidden');

        } catch (error) {
            console.error('Autocomplete error:', error);
            suggestionsDiv.classList.add('hidden');
        }
    }, 300);

    // Event listeners
    inputElement.addEventListener('input', function() {
        fetchSuggestions(this.value);
    });

    inputElement.addEventListener('focus', function() {
        if (this.value.length >= 2) {
            fetchSuggestions(this.value);
        }
    });

    // Hide suggestions when clicking outside
    document.addEventListener('click', function(event) {
        if (!inputElement.contains(event.target) && !suggestionsDiv.contains(event.target)) {
            suggestionsDiv.classList.add('hidden');
        }
    });

    // Keyboard navigation
    inputElement.addEventListener('keydown', function(event) {
        const items = suggestionsDiv.querySelectorAll('[data-value]');
        const activeItem = suggestionsDiv.querySelector('.bg-indigo-100');
        
        if (event.key === 'ArrowDown') {
            event.preventDefault();
            if (!activeItem) {
                items[0]?.classList.add('bg-indigo-100');
            } else {
                activeItem.classList.remove('bg-indigo-100');
                const next = activeItem.nextElementSibling;
                if (next) {
                    next.classList.add('bg-indigo-100');
                    next.scrollIntoView({ block: 'nearest' });
                }
            }
        } else if (event.key === 'ArrowUp') {
            event.preventDefault();
            if (activeItem) {
                activeItem.classList.remove('bg-indigo-100');
                const prev = activeItem.previousElementSibling;
                if (prev) {
                    prev.classList.add('bg-indigo-100');
                    prev.scrollIntoView({ block: 'nearest' });
                }
            }
        } else if (event.key === 'Enter') {
            if (activeItem) {
                event.preventDefault();
                inputElement.value = activeItem.dataset.value;
                suggestionsDiv.classList.add('hidden');
            }
        } else if (event.key === 'Escape') {
            suggestionsDiv.classList.add('hidden');
        }
    });
}

// Form validation
function validateSearchForm() {
    const firstName = document.getElementById('first_name').value.trim();
    const lastName = document.getElementById('last_name').value.trim();
    const county = document.getElementById('county').value.trim();
    const dateOfBirth = document.getElementById('date_of_birth').value;

    if (!firstName && !lastName && !county && !dateOfBirth) {
        showNotification('Please enter at least one search criterion', 'warning');
        return false;
    }

    return true;
}

// Notification helper
function showNotification(message, type = 'info') {
    const colors = {
        info: 'bg-blue-500',
        success: 'bg-green-500',
        warning: 'bg-yellow-500',
        error: 'bg-red-500'
    };

    const notification = document.createElement('div');
    notification.className = `fixed top-4 right-4 ${colors[type]} text-white px-6 py-3 rounded-lg shadow-lg z-50 animate-fade-in`;
    notification.textContent = message;

    document.body.appendChild(notification);

    setTimeout(() => {
        notification.classList.add('animate-fade-out');
        setTimeout(() => notification.remove(), 300);
    }, 3000);
}

// Export utility functions for testing
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        debounce,
        validateSearchForm,
        showNotification
    };
}
