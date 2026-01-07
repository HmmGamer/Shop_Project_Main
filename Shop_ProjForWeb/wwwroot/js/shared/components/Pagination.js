import { BaseComponent } from './BaseComponent.js';

/**
 * Pagination component
 */
export class Pagination extends BaseComponent {
    render() {
        const { currentPage = 1, totalPages = 1, onPageChange } = this.options;
        
        if (totalPages <= 1) return '';

        const pages = [];
        const maxVisiblePages = 5;

        let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2));
        let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);

        if (endPage - startPage < maxVisiblePages - 1) {
            startPage = Math.max(1, endPage - maxVisiblePages + 1);
        }

        // Previous button
        pages.push(`
            <button class="btn btn-outline pagination-btn ${currentPage === 1 ? 'disabled' : ''}" 
                    data-page="${currentPage - 1}" ${currentPage === 1 ? 'disabled' : ''}>
                Previous
            </button>
        `);

        // First page
        if (startPage > 1) {
            pages.push(`<button class="btn btn-outline pagination-btn" data-page="1">1</button>`);
            if (startPage > 2) {
                pages.push(`<span class="pagination-ellipsis">...</span>`);
            }
        }

        // Page numbers
        for (let i = startPage; i <= endPage; i++) {
            pages.push(`
                <button class="btn ${i === currentPage ? 'btn-primary' : 'btn-outline'} pagination-btn" 
                        data-page="${i}">${i}</button>
            `);
        }

        // Last page
        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                pages.push(`<span class="pagination-ellipsis">...</span>`);
            }
            pages.push(`<button class="btn btn-outline pagination-btn" data-page="${totalPages}">${totalPages}</button>`);
        }

        // Next button
        pages.push(`
            <button class="btn btn-outline pagination-btn ${currentPage === totalPages ? 'disabled' : ''}" 
                    data-page="${currentPage + 1}" ${currentPage === totalPages ? 'disabled' : ''}>
                Next
            </button>
        `);

        return `
            <div class="pagination">
                <div class="pagination-info">Page ${currentPage} of ${totalPages}</div>
                <div class="pagination-controls">${pages.join('')}</div>
            </div>
        `;
    }

    afterMount() {
        if (this.container && this.options.onPageChange) {
            this.container.querySelectorAll('.pagination-btn').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    const page = parseInt(e.target.dataset.page);
                    if (!isNaN(page)) {
                        this.options.onPageChange(page);
                    }
                });
            });
        }
    }
}
