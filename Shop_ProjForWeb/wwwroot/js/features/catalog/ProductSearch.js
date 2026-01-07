import { BaseComponent } from '../../shared/components/BaseComponent.js';
import { debounce } from '../../shared/utils/debounce.js';

/**
 * Product search component
 */
export class ProductSearch extends BaseComponent {
    constructor(container, options = {}) {
        super(container, options);
        this.debouncedSearch = debounce((query) => {
            if (this.options.onSearch) {
                this.options.onSearch(query);
            }
        }, 500);
    }

    render() {
        const { searchQuery = '', sortBy = 'name', sortDescending = false } = this.options;

        return `
            <div class="search-and-filters">
                <div class="search-section">
                    <div class="search-box">
                        <input type="text" class="form-input search-input" placeholder="Search products..." 
                               value="${searchQuery}" id="product-search">
                        <button class="btn btn-primary search-button" id="search-btn">Search</button>
                        ${searchQuery ? '<button class="btn btn-outline" id="clear-search-btn">Clear</button>' : ''}
                    </div>
                </div>
                <div class="sort-section">
                    <div class="sort-controls">
                        <label class="form-label">Sort by:</label>
                        <select class="form-select sort-select" id="sort-select">
                            <option value="name" ${sortBy === 'name' ? 'selected' : ''}>Name</option>
                            <option value="price" ${sortBy === 'price' ? 'selected' : ''}>Price</option>
                            <option value="createdat" ${sortBy === 'createdat' ? 'selected' : ''}>Date Added</option>
                        </select>
                        <button class="btn btn-outline sort-direction-btn" id="sort-direction-btn">
                            ${sortDescending ? '↓ Desc' : '↑ Asc'}
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    afterMount() {
        const searchInput = document.getElementById('product-search');
        const searchBtn = document.getElementById('search-btn');
        const clearBtn = document.getElementById('clear-search-btn');
        const sortSelect = document.getElementById('sort-select');
        const sortDirectionBtn = document.getElementById('sort-direction-btn');

        if (searchInput) {
            searchInput.oninput = (e) => this.debouncedSearch(e.target.value);
            searchInput.onkeypress = (e) => {
                if (e.key === 'Enter' && this.options.onSearch) {
                    this.options.onSearch(e.target.value);
                }
            };
        }

        if (searchBtn) {
            searchBtn.onclick = () => {
                if (this.options.onSearch) {
                    this.options.onSearch(searchInput?.value || '');
                }
            };
        }

        if (clearBtn) {
            clearBtn.onclick = () => {
                if (searchInput) searchInput.value = '';
                if (this.options.onClear) this.options.onClear();
            };
        }

        if (sortSelect && this.options.onSortChange) {
            sortSelect.onchange = (e) => this.options.onSortChange(e.target.value);
        }

        if (sortDirectionBtn && this.options.onSortDirectionChange) {
            sortDirectionBtn.onclick = () => this.options.onSortDirectionChange();
        }
    }
}
