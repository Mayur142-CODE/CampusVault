/* ============================================================
   CAMPUSVAULT — Interactive JavaScript
   Vanilla JS for all UI interactions
   ============================================================ */

(function () {
    'use strict';

    // ============================================================
    // HTML ESCAPING UTILITY
    // ============================================================
    function escapeHtml(str) {
        if (str === null || str === undefined) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }
    window.escapeHtml = escapeHtml;
    window.CampusVault = window.CampusVault || {};
    window.CampusVault.escapeHtml = escapeHtml;

    // ============================================================
    // SIDEBAR
    // ============================================================
    const app = document.querySelector('.cv-app');
    const sidebar = document.querySelector('.cv-sidebar');
    const sidebarToggle = document.querySelector('.cv-sidebar-toggle');
    const mobileToggle = document.querySelector('.cv-mobile-toggle');
    const sidebarOverlay = document.querySelector('.cv-sidebar-overlay');

    // Desktop collapse
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function () {
            app.classList.toggle('sidebar-collapsed');
            localStorage.setItem('cv-sidebar-collapsed', app.classList.contains('sidebar-collapsed'));
        });
    }

    // Restore sidebar state
    if (app && localStorage.getItem('cv-sidebar-collapsed') === 'true') {
        app.classList.add('sidebar-collapsed');
    }

    // Mobile sidebar
    if (mobileToggle) {
        mobileToggle.addEventListener('click', function () {
            sidebar.classList.toggle('mobile-open');
            if (sidebarOverlay) sidebarOverlay.classList.toggle('active');
            document.body.style.overflow = sidebar.classList.contains('mobile-open') ? 'hidden' : '';
        });
    }

    if (sidebarOverlay) {
        sidebarOverlay.addEventListener('click', function () {
            sidebar.classList.remove('mobile-open');
            sidebarOverlay.classList.remove('active');
            document.body.style.overflow = '';
        });
    }

    // Close mobile sidebar on window resize
    window.addEventListener('resize', function () {
        if (window.innerWidth > 768 && sidebar) {
            sidebar.classList.remove('mobile-open');
            if (sidebarOverlay) sidebarOverlay.classList.remove('active');
            document.body.style.overflow = '';
        }
    });

    // Toggle joined classrooms sub-list in sidebar
    window.toggleSidebarClassrooms = function (e) {
        if (e) {
            e.preventDefault();
            e.stopPropagation();
        }
        var subList = document.getElementById('sidebarClassroomsSub');
        var chevron = document.getElementById('classroomChevron');
        if (subList) {
            var isHidden = window.getComputedStyle(subList).display === 'none';
            subList.style.display = isHidden ? 'flex' : 'none';
            if (chevron) {
                chevron.style.transform = isHidden ? 'rotate(180deg)' : 'rotate(0deg)';
            }
        }
    };


    // ============================================================
    // DROPDOWNS (Generic & Profile Dropdown)
    // ============================================================
    function closeAllDropdowns() {
        document.querySelectorAll('.cv-dropdown-menu.open').forEach(function (m) {
            m.classList.remove('open');
        });
        document.querySelectorAll('[data-dropdown].open').forEach(function (t) {
            t.classList.remove('open');
            if (t.hasAttribute('aria-expanded')) {
                t.setAttribute('aria-expanded', 'false');
            }
        });
    }

    // Toggle dropdown on trigger click/tap
    document.addEventListener('click', function (e) {
        var trigger = e.target.closest('[data-dropdown]');
        if (trigger) {
            e.preventDefault();
            e.stopPropagation();
            var targetId = trigger.getAttribute('data-dropdown');
            var menu = document.getElementById(targetId);
            if (menu) {
                var isOpen = menu.classList.contains('open');
                closeAllDropdowns();
                if (!isOpen) {
                    menu.classList.add('open');
                    trigger.classList.add('open');
                    if (trigger.hasAttribute('aria-expanded')) {
                        trigger.setAttribute('aria-expanded', 'true');
                    }
                }
            }
            return;
        }

        // Close dropdowns when clicking outside
        if (!e.target.closest('.cv-dropdown-menu')) {
            closeAllDropdowns();
        }
    });

    // Close dropdowns on touch outside for mobile
    document.addEventListener('touchstart', function (e) {
        if (!e.target.closest('[data-dropdown]') && !e.target.closest('.cv-dropdown-menu')) {
            closeAllDropdowns();
        }
    }, { passive: true });

    // Keyboard support: Escape closes all open dropdowns
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' || e.key === 'Esc') {
            closeAllDropdowns();
        }
    });


    // ============================================================
    // TABS
    // ============================================================
    document.addEventListener('click', function (e) {
        var tab = e.target.closest('[data-tab]');
        if (!tab) return;

        e.preventDefault();
        var tabGroup = tab.closest('.cv-tabs, .cv-tabs-pill');
        if (!tabGroup) return;

        var targetId = tab.getAttribute('data-tab');

        // Deactivate all tabs in group
        tabGroup.querySelectorAll('.cv-tab').forEach(function (t) {
            t.classList.remove('active');
        });

        // Activate clicked tab
        tab.classList.add('active');

        // Find the tab content container
        var container = tabGroup.parentElement;
        container.querySelectorAll('.cv-tab-content').forEach(function (content) {
            content.classList.remove('active');
        });

        var targetContent = document.getElementById(targetId);
        if (targetContent) {
            targetContent.classList.add('active');
        }
    });


    // ============================================================
    // MODALS
    // ============================================================
    document.addEventListener('click', function (e) {
        // Open modal
        var modalTrigger = e.target.closest('[data-modal-open]');
        if (modalTrigger) {
            e.preventDefault();
            var modalId = modalTrigger.getAttribute('data-modal-open');
            openModal(modalId);
            return;
        }

        // Close modal
        var modalClose = e.target.closest('[data-modal-close]');
        if (modalClose) {
            e.preventDefault();
            var overlay = modalClose.closest('.cv-modal-overlay');
            if (overlay) closeModal(overlay);
            return;
        }

        // Close on overlay click
        if (e.target.classList.contains('cv-modal-overlay')) {
            closeModal(e.target);
        }
    });

    // Close modal on Escape
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            var openModal = document.querySelector('.cv-modal-overlay.open');
            if (openModal) closeModal(openModal);

            // Also close search
            var searchOverlay = document.querySelector('.cv-search-overlay.open');
            if (searchOverlay) closeSearch();
        }
    });

    function openModal(modalId) {
        var overlay = document.getElementById(modalId);
        if (overlay) {
            overlay.classList.add('open');
            document.body.style.overflow = 'hidden';
        }
    }

    function closeModal(overlay) {
        overlay.classList.remove('open');
        document.body.style.overflow = '';
    }

    // Expose globally
    window.CampusVault = window.CampusVault || {};
    window.CampusVault.openModal = openModal;
    window.CampusVault.closeModal = closeModal;


    // ============================================================
    // SEARCH / COMMAND PALETTE
    // ============================================================
    var searchOverlay = document.getElementById('searchOverlay') || document.querySelector('.cv-search-overlay');
    var searchInput = document.getElementById('globalSearchInput') || document.querySelector('.cv-search-input');
    var searchResults = document.getElementById('globalSearchResults') || document.querySelector('.cv-search-results');
    var searchClearBtn = document.getElementById('searchClearBtn');
    var searchFooterStatus = document.getElementById('searchFooterStatus');
    var searchCategoryPills = document.getElementById('searchCategoryPills');

    var currentCategoryFilter = 'all';
    var focusedItemIndex = -1;
    var currentResultElements = [];

    // Mock search database across 4 required categories: Classrooms, Tasks, Documents, Materials
    var mockSearchDatabase = [
        // --- CLASSROOMS ---
        {
            id: 'cls-1',
            type: 'Classroom',
            category: 'Classrooms',
            title: 'DBMS — EC6',
            code: 'CS-502',
            subtitle: 'Prof. Patel • 42 Enrolled • Semester 5',
            meta: 'Mon/Wed 10:00 AM • Lab 302',
            url: '/Student/Classrooms',
            icon: 'classroom'
        },
        {
            id: 'cls-2',
            type: 'Classroom',
            category: 'Classrooms',
            title: 'Data Structures — EC7',
            code: 'CS-501',
            subtitle: 'Prof. Shah • 58 Enrolled • Semester 5',
            meta: 'Tue/Thu 09:00 AM • Hall 104',
            url: '/Student/Classrooms',
            icon: 'classroom'
        },
        {
            id: 'cls-3',
            type: 'Classroom',
            category: 'Classrooms',
            title: 'Web Technology — EC6',
            code: 'CS-503',
            subtitle: 'Prof. Mehta • 60 Enrolled • Semester 5',
            meta: 'Wed/Fri 02:00 PM • Lab 201',
            url: '/Student/Classrooms',
            icon: 'classroom'
        },
        {
            id: 'cls-4',
            type: 'Classroom',
            category: 'Classrooms',
            title: 'Object Oriented Programming — EC6',
            code: 'CS-504',
            subtitle: 'Prof. Patel • 45 Enrolled • Semester 6',
            meta: 'Mon/Fri 11:30 AM • Hall 205',
            url: '/Student/Classrooms',
            icon: 'classroom'
        },

        // --- TASKS ---
        {
            id: 'tsk-1',
            type: 'Task',
            category: 'Tasks',
            title: 'DBMS Practical 4',
            code: 'CS-502',
            subtitle: 'DBMS — EC6 • Due Sep 24 • Normalization & BCNF',
            meta: 'Points: 25 • 36/42 Submitted',
            url: '/Student/Tasks',
            icon: 'task'
        },
        {
            id: 'tsk-2',
            type: 'Task',
            category: 'Tasks',
            title: 'DBMS Practical 3',
            code: 'CS-502',
            subtitle: 'DBMS — EC6 • Completed • SQL Joins & Subqueries',
            meta: 'Points: 20 • Graded',
            url: '/Student/Tasks',
            icon: 'task'
        },
        {
            id: 'tsk-3',
            type: 'Task',
            category: 'Tasks',
            title: 'Linked List Implementation',
            code: 'CS-501',
            subtitle: 'Data Structures — EC7 • Due Sep 18 • Singly & Doubly',
            meta: 'Points: 30 • Active',
            url: '/Student/Tasks',
            icon: 'task'
        },
        {
            id: 'tsk-4',
            type: 'Task',
            category: 'Tasks',
            title: 'Web Technology Mini Project',
            code: 'CS-503',
            subtitle: 'Web Technology — EC6 • Due Sep 25 • Full-Stack Vault',
            meta: 'Points: 50 • In Progress',
            url: '/Student/Tasks',
            icon: 'task'
        },
        {
            id: 'tsk-5',
            type: 'Task',
            category: 'Tasks',
            title: 'OOP Polymorphism & Templates',
            code: 'CS-504',
            subtitle: 'OOP — EC6 • Due Sep 20 • Virtual Functions',
            meta: 'Points: 25 • Active',
            url: '/Student/Tasks',
            icon: 'task'
        },

        // --- DOCUMENTS ---
        {
            id: 'doc-1',
            type: 'Document',
            category: 'Documents',
            title: 'DBMS_Practical_04.pdf',
            code: 'CS-502',
            subtitle: 'DBMS Practical 4 • Version 2.0 • Mayur Chavda',
            meta: '2.4 MB • Approved • Signed SHA-256',
            url: '/Student/Documents',
            icon: 'document'
        },
        {
            id: 'doc-2',
            type: 'Document',
            category: 'Documents',
            title: 'DBMS_ER_Schema_v1.pdf',
            code: 'CS-502',
            subtitle: 'Database Design • Version 1.1 • Final Submission',
            meta: '1.8 MB • Approved',
            url: '/Student/Documents',
            icon: 'document'
        },
        {
            id: 'doc-3',
            type: 'Document',
            category: 'Documents',
            title: 'LinkedList_Implementation.java',
            code: 'CS-501',
            subtitle: 'Data Structures • Version 1.0 • Source Code',
            meta: '14 KB • Verified',
            url: '/Student/Documents',
            icon: 'document'
        },
        {
            id: 'doc-4',
            type: 'Document',
            category: 'Documents',
            title: 'Web_MiniProject_Architecture.pdf',
            code: 'CS-503',
            subtitle: 'Web Technology • Version 1.0 • System Blueprint',
            meta: '3.1 MB • In Review',
            url: '/Student/Documents',
            icon: 'document'
        },
        {
            id: 'doc-5',
            type: 'Document',
            category: 'Documents',
            title: 'OOP_Virtual_Destructors_Report.pdf',
            code: 'CS-504',
            subtitle: 'OOP — EC6 • Version 1.2 • Lab Report',
            meta: '920 KB • Approved',
            url: '/Student/Documents',
            icon: 'document'
        },

        // --- MATERIALS ---
        {
            id: 'mat-1',
            type: 'Material',
            category: 'Materials',
            title: 'DBMS Unit 4 Notes',
            code: 'CS-502',
            subtitle: 'Prof. Patel • Relational Decomposition & Normal Forms',
            meta: 'PDF • 4.5 MB • Uploaded Sep 02',
            url: '/Student/Materials',
            icon: 'material'
        },
        {
            id: 'mat-2',
            type: 'Material',
            category: 'Materials',
            title: 'DBMS Question Bank 2026',
            code: 'CS-502',
            subtitle: 'Department of CE • 120 Exam Questions with Solutions',
            meta: 'PDF • 2.1 MB • Comprehensive',
            url: '/Student/Materials',
            icon: 'material'
        },
        {
            id: 'mat-3',
            type: 'Material',
            category: 'Materials',
            title: 'Data Structures Lecture Slides 1-10',
            code: 'CS-501',
            subtitle: 'Prof. Shah • Complete Tree & Graph Algorithms',
            meta: 'PPTX • 18.2 MB • Uploaded Aug 28',
            url: '/Student/Materials',
            icon: 'material'
        },
        {
            id: 'mat-4',
            type: 'Material',
            category: 'Materials',
            title: 'Web Tech CSS Grid & Flexbox Cheatsheet',
            code: 'CS-503',
            subtitle: 'Prof. Mehta • Modern Layout Guide & Snippets',
            meta: 'PDF • 950 KB • Reference',
            url: '/Student/Materials',
            icon: 'material'
        },
        {
            id: 'mat-5',
            type: 'Material',
            category: 'Materials',
            title: 'OOP Virtual Functions & Abstract Classes',
            code: 'CS-504',
            subtitle: 'Prof. Patel • C++ Polymorphism Lab Companion',
            meta: 'PDF • 1.4 MB • Uploaded Aug 30',
            url: '/Student/Materials',
            icon: 'material'
        }
    ];

    // Recent searches management (localStorage with default fallbacks)
    function getRecentSearches() {
        try {
            var raw = localStorage.getItem('cv-recent-searches');
            if (raw) {
                var parsed = JSON.parse(raw);
                if (Array.isArray(parsed) && parsed.length > 0) return parsed;
            }
        } catch (e) { }
        return ['DBMS', 'Normalization', 'Data Structures', 'Practical 4'];
    }

    function saveRecentSearches(list) {
        try {
            localStorage.setItem('cv-recent-searches', JSON.stringify(list));
        } catch (e) { }
    }

    function addRecentSearch(query) {
        if (!query || typeof query !== 'string') return;
        query = query.trim();
        if (!query) return;
        var list = getRecentSearches();
        list = list.filter(function (item) {
            return item.toLowerCase() !== query.toLowerCase();
        });
        list.unshift(query);
        if (list.length > 8) list.pop();
        saveRecentSearches(list);
    }

    function removeRecentSearch(query) {
        var list = getRecentSearches();
        list = list.filter(function (item) {
            return item.toLowerCase() !== query.toLowerCase();
        });
        saveRecentSearches(list);
        renderRecentSearches();
    }

    function clearAllRecentSearches() {
        saveRecentSearches([]);
        renderRecentSearches();
    }

    // Modal open / close
    function openSearch(initialQuery) {
        if (!searchOverlay) {
            searchOverlay = document.getElementById('searchOverlay') || document.querySelector('.cv-search-overlay');
        }
        if (searchOverlay) {
            searchOverlay.classList.add('open');
            document.body.style.overflow = 'hidden';
            if (!searchInput) {
                searchInput = document.getElementById('globalSearchInput') || document.querySelector('.cv-search-input');
            }
            if (searchInput) {
                if (typeof initialQuery === 'string') {
                    searchInput.value = initialQuery;
                }
                setTimeout(function () {
                    searchInput.focus();
                    if (searchInput.value) {
                        searchInput.select();
                        executeSearch(searchInput.value);
                    } else {
                        renderRecentSearches();
                    }
                }, 100);
            }
        }
    }

    function closeSearch() {
        if (!searchOverlay) {
            searchOverlay = document.getElementById('searchOverlay') || document.querySelector('.cv-search-overlay');
        }
        if (searchOverlay) {
            searchOverlay.classList.remove('open');
            document.body.style.overflow = '';
            if (searchInput) {
                searchInput.value = '';
            }
            if (searchClearBtn) {
                searchClearBtn.style.display = 'none';
            }
            focusedItemIndex = -1;
            currentResultElements = [];
        }
    }

    function clearSearch() {
        if (searchInput) {
            searchInput.value = '';
            searchInput.focus();
        }
        if (searchClearBtn) {
            searchClearBtn.style.display = 'none';
        }
        focusedItemIndex = -1;
        currentResultElements = [];
        renderRecentSearches();
    }

    // Render Recent Searches View
    function renderRecentSearches() {
        if (!searchResults) {
            searchResults = document.getElementById('globalSearchResults') || document.querySelector('.cv-search-results');
        }
        if (!searchResults) return;

        var recents = getRecentSearches();
        var recentsHtml = '';

        if (recents && recents.length > 0) {
            recentsHtml = '<div class="cv-search-recent-section">' +
                '<div class="cv-search-recent-head">' +
                '<span>Recent Searches</span>' +
                '<button type="button" class="cv-search-recent-clear-btn" onclick="CampusVault.clearRecentSearches()">Clear all</button>' +
                '</div>' +
                '<div class="cv-search-recent-chips">';

            recents.forEach(function (term) {
                recentsHtml += '<div class="cv-search-recent-chip" onclick="CampusVault.applySearchTerm(\'' + escapeHtml(term) + '\')">' +
                    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>' +
                    '<span>' + escapeHtml(term) + '</span>' +
                    '<span class="chip-remove" title="Remove" onclick="event.stopPropagation(); CampusVault.removeRecentSearch(\'' + escapeHtml(term) + '\')">&times;</span>' +
                    '</div>';
            });

            recentsHtml += '</div></div>';
        } else {
            recentsHtml = '<div class="cv-search-recent-section">' +
                '<div class="cv-search-recent-head"><span>Suggested Quick Searches</span></div>' +
                '<div class="cv-search-recent-chips">' +
                '<div class="cv-search-recent-chip" onclick="CampusVault.applySearchTerm(\'DBMS\')"><span>DBMS</span></div>' +
                '<div class="cv-search-recent-chip" onclick="CampusVault.applySearchTerm(\'Data Structures\')"><span>Data Structures</span></div>' +
                '<div class="cv-search-recent-chip" onclick="CampusVault.applySearchTerm(\'Practical\')"><span>Practical</span></div>' +
                '<div class="cv-search-recent-chip" onclick="CampusVault.applySearchTerm(\'Web Technology\')"><span>Web Technology</span></div>' +
                '</div></div>';
        }

        // Quick Categories Shortcuts
        var quickCategoriesHtml = '<div class="cv-search-quick-links">' +
            '<div class="cv-search-recent-head" style="margin-bottom:6px;"><span>Browse by Category</span></div>' +
            '<div class="cv-search-quick-grid">' +
            '<div class="cv-search-quick-card" onclick="CampusVault.filterCategory(\'Classroom\')">' +
            '<div class="result-icon-box icon-classroom" style="width:28px;height:28px;"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/></svg></div>' +
            '<span class="card-title">Classrooms</span>' +
            '<span class="card-count">4 rooms</span>' +
            '</div>' +
            '<div class="cv-search-quick-card" onclick="CampusVault.filterCategory(\'Task\')">' +
            '<div class="result-icon-box icon-task" style="width:28px;height:28px;"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"/><rect x="8" y="2" width="8" height="4" rx="1" ry="1"/></svg></div>' +
            '<span class="card-title">Tasks</span>' +
            '<span class="card-count">5 tasks</span>' +
            '</div>' +
            '<div class="cv-search-quick-card" onclick="CampusVault.filterCategory(\'Document\')">' +
            '<div class="result-icon-box icon-document" style="width:28px;height:28px;"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg></div>' +
            '<span class="card-title">Documents</span>' +
            '<span class="card-count">5 files</span>' +
            '</div>' +
            '<div class="cv-search-quick-card" onclick="CampusVault.filterCategory(\'Material\')">' +
            '<div class="result-icon-box icon-material" style="width:28px;height:28px;"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/></svg></div>' +
            '<span class="card-title">Materials</span>' +
            '<span class="card-count">5 items</span>' +
            '</div>' +
            '</div></div>';

        searchResults.innerHTML = recentsHtml + quickCategoriesHtml;

        if (searchFooterStatus) {
            searchFooterStatus.textContent = 'Type to search or select category';
        }
        focusedItemIndex = -1;
        currentResultElements = [];
    }

    // Execute Search with Category Grouping
    function executeSearch(query) {
        if (!searchResults) {
            searchResults = document.getElementById('globalSearchResults') || document.querySelector('.cv-search-results');
        }
        if (!searchResults) return;

        var q = (query || '').trim().toLowerCase();

        if (searchClearBtn) {
            searchClearBtn.style.display = q ? 'inline-flex' : 'none';
        }

        if (!q) {
            if (currentCategoryFilter === 'all') {
                renderRecentSearches();
                return;
            }
        }

        // Filter items
        var filtered = mockSearchDatabase.filter(function (item) {
            // Category filter
            if (currentCategoryFilter !== 'all' && item.type !== currentCategoryFilter) {
                return false;
            }
            if (!q) return true; // if filtering by category without text
            return item.title.toLowerCase().indexOf(q) !== -1 ||
                item.subtitle.toLowerCase().indexOf(q) !== -1 ||
                (item.code && item.code.toLowerCase().indexOf(q) !== -1) ||
                (item.meta && item.meta.toLowerCase().indexOf(q) !== -1) ||
                item.type.toLowerCase().indexOf(q) !== -1;
        });

        if (filtered.length === 0) {
            searchResults.innerHTML = '<div class="cv-search-empty-state">' +
                '<div class="cv-search-empty-icon">' +
                '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>' +
                '</div>' +
                '<h4 class="cv-search-empty-title">No matching results</h4>' +
                '<p class="cv-search-empty-desc">We couldn\'t find any classrooms, tasks, documents, or materials matching "<strong>' + escapeHtml(query) + '</strong>".</p>' +
                '</div>';

            if (searchFooterStatus) {
                searchFooterStatus.textContent = '0 results';
            }
            focusedItemIndex = -1;
            currentResultElements = [];
            return;
        }

        // Group into order: Classrooms, Tasks, Documents, Materials
        var categoryOrder = ['Classroom', 'Task', 'Document', 'Material'];
        var categoryLabels = {
            'Classroom': 'Classrooms',
            'Task': 'Tasks',
            'Document': 'Documents',
            'Material': 'Materials'
        };

        var grouped = {};
        categoryOrder.forEach(function (cat) {
            grouped[cat] = [];
        });

        filtered.forEach(function (item) {
            if (grouped[item.type]) {
                grouped[item.type].push(item);
            }
        });

        var html = '';
        var itemCounter = 0;

        categoryOrder.forEach(function (cat) {
            var items = grouped[cat];
            if (items && items.length > 0) {
                html += '<div class="cv-search-group">' +
                    '<div class="cv-search-group-header">' +
                    '<span>' + categoryLabels[cat] + '</span>' +
                    '<span class="group-badge">' + items.length + '</span>' +
                    '</div>';

                items.forEach(function (item) {
                    var iconClass = 'icon-' + item.type.toLowerCase();
                    var iconSvg = getSearchCategoryIcon(item.type);

                    html += '<div class="cv-search-result-item" data-index="' + itemCounter + '" data-id="' + item.id + '" onclick="CampusVault.selectSearchResult(\'' + item.id + '\')">' +
                        '<div class="result-icon-box ' + iconClass + '">' + iconSvg + '</div>' +
                        '<div class="result-body">' +
                        '<div class="result-title">' +
                        highlightText(item.title, q) +
                        (item.code ? '<span class="result-meta-tag">' + escapeHtml(item.code) + '</span>' : '') +
                        '</div>' +
                        '<div class="result-subtitle">' + escapeHtml(item.subtitle) + ' &bull; <span style="color:var(--cv-slate-light);">' + escapeHtml(item.meta) + '</span></div>' +
                        '</div>' +
                        '<span class="result-action-hint">Open &rarr; <kbd>&crarr;</kbd></span>' +
                        '</div>';

                    itemCounter++;
                });

                html += '</div>';
            }
        });

        searchResults.innerHTML = html;

        // Cache elements for arrow navigation
        currentResultElements = Array.from(searchResults.querySelectorAll('.cv-search-result-item'));
        focusedItemIndex = currentResultElements.length > 0 ? 0 : -1;
        updateFocusedItem();

        if (searchFooterStatus) {
            searchFooterStatus.textContent = filtered.length + (filtered.length === 1 ? ' result' : ' results');
        }
    }

    function getSearchCategoryIcon(type) {
        switch (type) {
            case 'Classroom':
                return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/></svg>';
            case 'Task':
                return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"/><rect x="8" y="2" width="8" height="4" rx="1" ry="1"/></svg>';
            case 'Document':
                return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>';
            case 'Material':
                return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/></svg>';
            default:
                return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/></svg>';
        }
    }

    function highlightText(text, query) {
        if (!query) return escapeHtml(text);
        var idx = text.toLowerCase().indexOf(query);
        if (idx === -1) return escapeHtml(text);
        var before = text.substring(0, idx);
        var match = text.substring(idx, idx + query.length);
        var after = text.substring(idx + query.length);
        return escapeHtml(before) + '<strong>' + escapeHtml(match) + '</strong>' + highlightText(after, query);
    }

    function updateFocusedItem() {
        if (!currentResultElements || currentResultElements.length === 0) return;
        currentResultElements.forEach(function (el, idx) {
            if (idx === focusedItemIndex) {
                el.classList.add('focused');
                el.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
            } else {
                el.classList.remove('focused');
            }
        });
    }

    function selectSearchResult(id) {
        var item = mockSearchDatabase.find(function (x) { return x.id === id; });
        if (item) {
            openSearchResult(item);
        }
    }

    function openSearchResult(item) {
        if (!item) return;

        addRecentSearch(item.title);
        closeSearch();

        var currentPath = window.location.pathname.toLowerCase();
        var rolePrefix = '/Student';
        if (currentPath.indexOf('/faculty') !== -1) {
            rolePrefix = '/Faculty';
        } else if (currentPath.indexOf('/admin') !== -1) {
            rolePrefix = '/Admin';
        }

        var targetUrl = item.url;
        if (rolePrefix === '/Faculty') {
            if (item.type === 'Classroom') targetUrl = '/Faculty/Classrooms';
            else if (item.type === 'Task') targetUrl = '/Faculty/Tasks';
            else if (item.type === 'Document') targetUrl = '/Faculty/Submissions';
            else if (item.type === 'Material') targetUrl = '/Faculty/Materials';
        } else if (rolePrefix === '/Admin') {
            if (item.type === 'Classroom') targetUrl = '/Admin/Classrooms';
            else if (item.type === 'Task') targetUrl = '/Admin/Tasks';
            else if (item.type === 'Document') targetUrl = '/Admin/Documents';
            else if (item.type === 'Material') targetUrl = '/Admin/Dashboard';
        }

        if (window.CampusVault && window.CampusVault.showToast) {
            window.CampusVault.showToast({
                type: 'info',
                title: item.type + ' Selected',
                message: 'Opening ' + item.title + '...',
                duration: 2500
            });
        }

        setTimeout(function () {
            window.location.href = targetUrl;
        }, 350);
    }

    // Attach Keyboard & Input listeners
    if (searchInput) {
        searchInput.addEventListener('input', function () {
            executeSearch(this.value);
        });

        searchInput.addEventListener('keydown', function (e) {
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                if (currentResultElements.length > 0) {
                    focusedItemIndex = (focusedItemIndex + 1) % currentResultElements.length;
                    updateFocusedItem();
                }
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                if (currentResultElements.length > 0) {
                    focusedItemIndex = (focusedItemIndex - 1 + currentResultElements.length) % currentResultElements.length;
                    updateFocusedItem();
                }
            } else if (e.key === 'Enter') {
                e.preventDefault();
                if (currentResultElements.length > 0 && focusedItemIndex >= 0 && focusedItemIndex < currentResultElements.length) {
                    var el = currentResultElements[focusedItemIndex];
                    var id = el.getAttribute('data-id');
                    selectSearchResult(id);
                } else if (this.value) {
                    addRecentSearch(this.value);
                }
            } else if (e.key === 'Escape') {
                e.preventDefault();
                closeSearch();
            }
        });
    }

    // Category pills click handler
    document.addEventListener('click', function (e) {
        var pill = e.target.closest('.cv-search-cat-pill');
        if (pill) {
            var cat = pill.getAttribute('data-category');
            setCategoryFilter(cat);
        }
    });

    function setCategoryFilter(category) {
        currentCategoryFilter = category || 'all';
        document.querySelectorAll('.cv-search-cat-pill').forEach(function (btn) {
            if (btn.getAttribute('data-category') === currentCategoryFilter) {
                btn.classList.add('active');
            } else {
                btn.classList.remove('active');
            }
        });
        if (searchInput) {
            executeSearch(searchInput.value);
            searchInput.focus();
        }
    }

    function filterCategory(category) {
        setCategoryFilter(category);
    }

    function applySearchTerm(term) {
        if (!searchInput) {
            searchInput = document.getElementById('globalSearchInput') || document.querySelector('.cv-search-input');
        }
        if (searchInput) {
            searchInput.value = term;
            executeSearch(term);
            searchInput.focus();
        }
    }

    // Global keyboard shortcuts: Ctrl+K / Cmd+K and /
    document.addEventListener('keydown', function (e) {
        // Ctrl+K or Cmd+K
        if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
            e.preventDefault();
            if (searchOverlay && searchOverlay.classList.contains('open')) {
                closeSearch();
            } else {
                openSearch();
            }
            return;
        }

        // Forward slash '/' when not inside an input, textarea, or contenteditable
        if (e.key === '/' && !e.ctrlKey && !e.metaKey && !e.altKey) {
            var tag = (e.target.tagName || '').toLowerCase();
            if (tag !== 'input' && tag !== 'textarea' && !e.target.isContentEditable) {
                e.preventDefault();
                openSearch();
            }
        }

        // Global Escape to close
        if (e.key === 'Escape') {
            if (searchOverlay && searchOverlay.classList.contains('open')) {
                closeSearch();
            }
        }
    });

    // Close on overlay backdrop click
    if (searchOverlay) {
        searchOverlay.addEventListener('click', function (e) {
            if (e.target === searchOverlay) {
                closeSearch();
            }
        });
    }

    // Expose search API on window.CampusVault
    window.CampusVault = window.CampusVault || {};
    window.CampusVault.openSearch = openSearch;
    window.CampusVault.closeSearch = closeSearch;
    window.CampusVault.clearSearch = clearSearch;
    window.CampusVault.clearRecentSearches = clearAllRecentSearches;
    window.CampusVault.removeRecentSearch = removeRecentSearch;
    window.CampusVault.applySearchTerm = applySearchTerm;
    window.CampusVault.filterCategory = filterCategory;
    window.CampusVault.selectSearchResult = selectSearchResult;


    // ============================================================
    // TOAST NOTIFICATIONS
    // ============================================================
    var toastCounter = 0;

    function showToast(options) {
        var container = document.querySelector('.cv-toast-container');
        if (!container) {
            container = document.createElement('div');
            container.className = 'cv-toast-container';
            document.body.appendChild(container);
        }

        var type = options.type || 'info';
        var title = options.title || '';
        var message = options.message || '';
        var duration = options.duration !== undefined ? options.duration : 5000;

        var toastId = 'toast-' + (++toastCounter);
        var iconSvg = getToastIcon(type);

        var toast = document.createElement('div');
        toast.className = 'cv-toast toast-' + type;
        toast.id = toastId;
        toast.innerHTML =
            '<div class="cv-toast-icon">' + iconSvg + '</div>' +
            '<div class="cv-toast-body">' +
            (title ? '<div class="cv-toast-title">' + escapeHtml(title) + '</div>' : '') +
            (message ? '<div class="cv-toast-message">' + escapeHtml(message) + '</div>' : '') +
            '</div>' +
            '<button class="cv-toast-close" onclick="CampusVault.dismissToast(\'' + toastId + '\')">' +
            '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>' +
            '</button>';

        container.appendChild(toast);

        if (duration > 0) {
            setTimeout(function () {
                dismissToast(toastId);
            }, duration);
        }

        return toastId;
    }

    function dismissToast(toastId) {
        var toast = document.getElementById(toastId);
        if (toast) {
            toast.classList.add('removing');
            setTimeout(function () {
                if (toast.parentNode) toast.parentNode.removeChild(toast);
            }, 300);
        }
    }

    function getToastIcon(type) {
        var icons = {
            success: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>',
            danger: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>',
            warning: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>',
            info: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>'
        };
        return icons[type] || icons.info;
    }

    window.CampusVault.showToast = showToast;
    window.CampusVault.dismissToast = dismissToast;


    // ============================================================
    // UPLOAD AREA
    // ============================================================
    document.querySelectorAll('.cv-upload-area').forEach(function (area) {
        var input = area.querySelector('input[type="file"]');
        var previewContainer = area.parentElement.querySelector('.cv-upload-previews');

        // Click to upload
        area.addEventListener('click', function () {
            if (input) input.click();
        });

        // Drag events
        area.addEventListener('dragover', function (e) {
            e.preventDefault();
            area.classList.add('drag-over');
        });

        area.addEventListener('dragleave', function (e) {
            e.preventDefault();
            area.classList.remove('drag-over');
        });

        area.addEventListener('drop', function (e) {
            e.preventDefault();
            area.classList.remove('drag-over');
            if (e.dataTransfer.files.length > 0) {
                handleFiles(e.dataTransfer.files, previewContainer);
            }
        });

        // File input change
        if (input) {
            input.addEventListener('change', function () {
                if (this.files.length > 0) {
                    handleFiles(this.files, previewContainer);
                }
            });
        }
    });

    function handleFiles(files, previewContainer) {
        if (!previewContainer) return;

        Array.from(files).forEach(function (file) {
            var preview = document.createElement('div');
            preview.className = 'cv-upload-preview';

            var ext = file.name.split('.').pop().toLowerCase();
            var iconClass = 'doc-pdf';
            if (['doc', 'docx'].includes(ext)) iconClass = 'doc-word';
            else if (['jpg', 'jpeg', 'png', 'gif'].includes(ext)) iconClass = 'doc-image';

            var sizeStr = formatFileSize(file.size);

            preview.innerHTML =
                '<div class="file-icon ' + iconClass + '">' +
                '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>' +
                '</div>' +
                '<div class="file-info">' +
                '<div class="file-name">' + escapeHtml(file.name) + '</div>' +
                '<div class="file-size">' + sizeStr + '</div>' +
                '</div>' +
                '<button class="file-remove" title="Remove file">' +
                '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>' +
                '</button>';

            preview.querySelector('.file-remove').addEventListener('click', function (e) {
                e.stopPropagation();
                preview.remove();
            });

            previewContainer.appendChild(preview);
        });

        showToast({
            type: 'success',
            title: 'File Selected',
            message: files.length + ' file(s) ready to upload.',
            duration: 3000
        });
    }

    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        var k = 1024;
        var sizes = ['Bytes', 'KB', 'MB', 'GB'];
        var i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
    }


    // ============================================================
    // CONFIRMATION DIALOG
    // ============================================================
    function confirmAction(options) {
        var title = options.title || 'Confirm Action';
        var message = options.message || 'Are you sure you want to proceed?';
        var confirmText = options.confirmText || 'Confirm';
        var cancelText = options.cancelText || 'Cancel';
        var type = options.type || 'danger';
        var onConfirm = options.onConfirm || function () {};

        var overlay = document.createElement('div');
        overlay.className = 'cv-modal-overlay open';
        overlay.innerHTML =
            '<div class="cv-modal" style="max-width: 420px;">' +
            '<div class="cv-modal-header">' +
            '<h3>' + escapeHtml(title) + '</h3>' +
            '<button class="cv-modal-close" data-dismiss="confirm">' +
            '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>' +
            '</button>' +
            '</div>' +
            '<div class="cv-modal-body">' +
            '<p>' + escapeHtml(message) + '</p>' +
            '</div>' +
            '<div class="cv-modal-footer">' +
            '<button class="cv-btn cv-btn-secondary" data-dismiss="confirm">' + escapeHtml(cancelText) + '</button>' +
            '<button class="cv-btn cv-btn-' + type + '" data-confirm="true">' + escapeHtml(confirmText) + '</button>' +
            '</div>' +
            '</div>';

        document.body.appendChild(overlay);
        document.body.style.overflow = 'hidden';

        overlay.querySelectorAll('[data-dismiss="confirm"]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                overlay.classList.remove('open');
                setTimeout(function () {
                    overlay.remove();
                    document.body.style.overflow = '';
                }, 250);
            });
        });

        overlay.querySelector('[data-confirm="true"]').addEventListener('click', function () {
            onConfirm();
            overlay.classList.remove('open');
            setTimeout(function () {
                overlay.remove();
                document.body.style.overflow = '';
            }, 250);
        });

        overlay.addEventListener('click', function (e) {
            if (e.target === overlay) {
                overlay.classList.remove('open');
                setTimeout(function () {
                    overlay.remove();
                    document.body.style.overflow = '';
                }, 250);
            }
        });
    }

    window.CampusVault.confirmAction = confirmAction;


    // ============================================================
    // LOADING BUTTON STATE
    // ============================================================
    function setButtonLoading(btn, loading) {
        if (loading) {
            btn.classList.add('loading');
            btn.disabled = true;
            btn.setAttribute('data-original-text', btn.innerHTML);
        } else {
            btn.classList.remove('loading');
            btn.disabled = false;
            var original = btn.getAttribute('data-original-text');
            if (original) btn.innerHTML = original;
        }
    }

    window.CampusVault.setButtonLoading = setButtonLoading;


    // ============================================================
    // ACTIVE NAV ITEM DETECTION
    // ============================================================
    (function () {
        var currentPath = window.location.pathname.toLowerCase();
        document.querySelectorAll('.cv-nav-item').forEach(function (item) {
            var href = item.getAttribute('href');
            if (href) {
                var itemPath = href.toLowerCase();
                if (currentPath === itemPath || currentPath.startsWith(itemPath + '/')) {
                    item.classList.add('active');
                }
            }
        });
    })();


    // ============================================================
    // ALERT DISMISS
    // ============================================================
    document.addEventListener('click', function (e) {
        var alertClose = e.target.closest('.cv-alert-close');
        if (alertClose) {
            var alert = alertClose.closest('.cv-alert');
            if (alert) {
                alert.style.opacity = '0';
                alert.style.transform = 'translateY(-8px)';
                alert.style.transition = 'all 0.3s ease';
                setTimeout(function () {
                    alert.remove();
                }, 300);
            }
        }
    });


    // ============================================================
    // PROGRESS BAR ANIMATION
    // ============================================================
    function animateProgress(selector, targetWidth) {
        var bar = document.querySelector(selector);
        if (bar) {
            bar.style.width = '0';
            requestAnimationFrame(function () {
                bar.style.width = targetWidth;
            });
        }
    }

    window.CampusVault.animateProgress = animateProgress;


    // ============================================================
    // SMOOTH SCROLL FOR PUBLIC PAGES
    // ============================================================
    document.querySelectorAll('a[href^="#"]').forEach(function (anchor) {
        anchor.addEventListener('click', function (e) {
            var target = this.getAttribute('href');
            if (target === '#') return;
            var el = document.querySelector(target);
            if (el) {
                e.preventDefault();
                el.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        });
    });


    // ============================================================
    // MOBILE NAV FOR PUBLIC PAGES
    // ============================================================
    var publicMobileBtn = document.querySelector('.cv-public-mobile-toggle');
    var publicNav = document.querySelector('.cv-public-nav .nav-links');

    if (publicMobileBtn && publicNav) {
        publicMobileBtn.addEventListener('click', function () {
            publicNav.classList.toggle('mobile-open');
        });
    }

})();
