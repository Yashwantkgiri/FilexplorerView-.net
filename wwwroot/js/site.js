// Complete Enhanced site.js - File Explorer with improved reliability
(function () {
    'use strict';

    // Check if FileExplorer already exists to prevent redeclaration
    if (window.FileExplorer) {
        return;
    }

    class FileExplorer {
        constructor() {
            this.activeFolderNode = null;
            this.selectedTags = [];
            this.loadingStates = new Set();
            this.init();
        }

        init() {
            this.bindEvents();
            this.setupErrorBoundary();
        }

        // Centralized error handling
        setupErrorBoundary() {
            window.addEventListener('unhandledrejection', (event) => {
                console.error('Unhandled promise rejection:', event.reason);
                this.showError('An unexpected error occurred. Please try again.');
                event.preventDefault();
            });
        }

        // Sanitize HTML to prevent XSS
        sanitizeHTML(str) {
            if (!str) return '';
            const div = document.createElement('div');
            div.textContent = str;
            return div.innerHTML;
        }

        // Validate file paths
        isValidPath(path) {
            if (!path || typeof path !== 'string') return false;
            const normalizedPath = path.replace(/\\/g, '/');
            return !normalizedPath.includes('../') && !normalizedPath.includes('..\\');
        }

        // Show loading state
        setLoading(elementId, isLoading) {
            const element = document.getElementById(elementId);
            if (!element) return;

            if (isLoading) {
                this.loadingStates.add(elementId);
                element.innerHTML = '<div class="d-flex justify-content-center p-4"><div class="spinner-border" role="status"><span class="visually-hidden">Loading...</span></div></div>';
            } else {
                this.loadingStates.delete(elementId);
            }
        }

        // Enhanced error display
        showError(message, elementId = 'main-content-area') {
            const element = document.getElementById(elementId);
            if (element) {
                element.innerHTML = `
                    <div class="alert alert-danger alert-dismissible fade show" role="alert">
                        <i class="fas fa-exclamation-triangle me-2"></i>
                        ${this.sanitizeHTML(message)}
                        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                    </div>`;
            }
        }

        // Success message display
        showSuccess(message) {
            const contentArea = document.getElementById('main-content-area');
            if (!contentArea) return;

            const successAlert = document.createElement('div');
            successAlert.className = 'alert alert-success alert-dismissible fade show';
            successAlert.innerHTML = `
                <i class="fas fa-check-circle me-2"></i>
                ${this.sanitizeHTML(message)}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            `;

            contentArea.insertBefore(successAlert, contentArea.firstChild);

            // Auto-dismiss after 3 seconds
            setTimeout(() => {
                if (successAlert.parentNode) {
                    successAlert.remove();
                }
            }, 3000);
        }

        // Improved folder contents loading with proper error handling
        async loadFolderContents(path) {
            if (!this.isValidPath(path)) {
                this.showError('Invalid folder path provided.');
                return;
            }

            this.setLoading('main-content-area', true);

            try {
                const response = await fetch(`/api/file/folder-contents?path=${encodeURIComponent(path)}`, {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });

                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }

                const files = await response.json();
                this.renderFolderContents(files, path);
            } catch (error) {
                console.error('Error loading folder contents:', error);
                this.showError(`Failed to load folder contents: ${error.message}`);
            } finally {
                this.setLoading('main-content-area', false);
            }
        }

        // Render folder contents table
        renderFolderContents(files, folderPath) {
            const mainContentArea = document.getElementById('main-content-area');
            if (!mainContentArea) return;

            const tableRows = files.length > 0 ? files.map(file => `
                <tr class="border-b hover:bg-gray-50">
                    <td class="p-3">
                        <i class="fas fa-file-alt me-2 text-gray-500"></i>
                        ${this.sanitizeHTML(file.name || file.Name)}
                    </td>
                    <td class="p-3">${file.formattedSize || file.FormattedSize || this.formatFileSize(file.size || file.Size || 0)}</td>
                    <td class="p-3">${new Date(file.modified || file.Modified).toLocaleString()}</td>
                    <td class="p-3 text-end">
                        <button class="view-btn btn btn-sm btn-outline-primary me-2" data-path="${this.sanitizeHTML(file.fullPath || file.FullPath)}">
                            <i class="fas fa-eye"></i> View
                        </button>
                        <button class="delete-btn btn btn-sm btn-outline-danger" data-path="${this.sanitizeHTML(file.fullPath || file.FullPath)}" data-name="${this.sanitizeHTML(file.name || file.Name)}">
                            <i class="fas fa-trash"></i> Delete
                        </button>
                    </td>
                </tr>
            `).join('') : `<tr><td colspan="4" class="p-4 text-center text-muted">This folder is empty.</td></tr>`;

            const folderName = folderPath.split('\\').pop() || folderPath.split('/').pop() || folderPath;

            mainContentArea.innerHTML = `
                <div class="d-flex justify-content-between align-items-center mb-3">
                    <h2 class="h5 text-truncate" title="${this.sanitizeHTML(folderPath)}">
                        Contents: <span class="text-primary monospace">${this.sanitizeHTML(folderName)}</span>
                    </h2>
                    <div>
                        <button id="add-file-btn" class="btn btn-success btn-sm me-2">
                            <i class="fas fa-plus"></i> Add File
                        </button>
                        <button id="create-file-btn" class="btn btn-primary btn-sm">
                            <i class="fas fa-file-medical"></i> Create New File
                        </button>
                    </div>
                </div>
                <div class="bg-white shadow rounded overflow-hidden">
                    <table class="table table-hover mb-0">
                        <thead class="table-light">
                            <tr>
                                <th>Name</th>
                                <th>Size</th>
                                <th>Last Modified</th>
                                <th class="text-end">Actions</th>
                            </tr>
                        </thead>
                        <tbody>${tableRows}</tbody>
                    </table>
                </div>
                <input type="file" id="file-upload-input" class="d-none" data-folder-path="${this.sanitizeHTML(folderPath)}" />
            `;

            this.setupFileUploadHandlers(folderPath);
        }

        // Setup file upload event handlers
        setupFileUploadHandlers(folderPath) {
            const fileInput = document.getElementById('file-upload-input');
            const addFileBtn = document.getElementById('add-file-btn');
            const createFileBtn = document.getElementById('create-file-btn');

            if (addFileBtn) {
                addFileBtn.onclick = () => {
                    if (fileInput) {
                        fileInput.value = "";
                        fileInput.click();
                    }
                };
            }

            if (createFileBtn) {
                createFileBtn.onclick = () => this.handleCreateFile(folderPath);
            }

            if (fileInput) {
                fileInput.onchange = () => this.handleFileUpload(fileInput, folderPath);
            }
        }

        // Handle file upload
        async handleFileUpload(fileInput, folderPath) {
            const file = fileInput.files[0];
            if (!file) return;

            try {
                const formData = new FormData();
                formData.append("file", file);
                formData.append("folderPath", folderPath);

                const response = await fetch('/api/file/upload-file', {
                    method: 'POST',
                    body: formData
                });

                let data = {};
                try {
                    data = await response.json();
                } catch {
                    // Ignore JSON parse errors
                }

                if (response.status === 409) {
                    alert(`File already exists: ${file.name}`);
                    return;
                }
                if (!response.ok) {
                    throw new Error(data?.message || "Upload failed.");
                }

                this.showSuccess(data.message || "File uploaded successfully!");
                await this.loadFolderContents(folderPath);
            } catch (err) {
                this.showError(`Error uploading file: ${err.message}`);
            }
        }

        // Handle create new file
        async handleCreateFile(folderPath) {
            const fileName = prompt("Enter new file name (with extension, e.g., test.txt):");
            if (!fileName) return;

            try {
                const formData = new FormData();
                formData.append("folderPath", folderPath);
                formData.append("fileName", fileName);

                const response = await fetch('/api/File/create-file', {
                    method: 'POST',
                    body: formData
                });

                const data = await response.json();

                if (!response.ok) {
                    alert(data.message || "Failed to create file.");
                    return;
                }

                this.showSuccess(data.message);

                // Load the new file immediately
                const newFilePath = folderPath +
                    (folderPath.endsWith("\\") || folderPath.endsWith("/") ? "" : "/") +
                    fileName;
                await this.loadFileForViewing(newFilePath);

                // Refresh folder contents
                await this.loadFolderContents(folderPath);

            } catch (err) {
                this.showError(`Error creating file: ${err.message}`);
            }
        }


        // Enhanced file loading with proper error handling
        async loadFileForViewing(path) {
            if (!this.isValidPath(path)) {
                this.showError('Invalid file path provided.');
                return;
            }

            this.setLoading('main-content-area', true);

            try {
                const response = await fetch(`/api/file/view-file?path=${encodeURIComponent(path)}`, {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });

                if (!response.ok) {
                    if (response.status === 404) {
                        throw new Error('File not found or has been deleted.');
                    }
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }

                const file = await response.json();
                this.renderFileViewer(file);
            } catch (error) {
                console.error('Error loading file:', error);
                this.showError(`Failed to load file: ${error.message}`);
            } finally {
                this.setLoading('main-content-area', false);
            }
        }

        // Render file viewer with syntax highlighting
        renderFileViewer(file) {
            const mainContentArea = document.getElementById('main-content-area');
            if (!mainContentArea) return;

            const content = file.content || file.Content || "";
            const filePath = file.fullPath || file.FullPath || "";
            const ext = filePath.split('.').pop()?.toLowerCase() || '';

            mainContentArea.innerHTML = `
                <div id="editor-container" class="editor-container d-flex flex-column h-100">
                    <!-- Tag details panel -->
                    <div id="tag-details" class="bg-light p-2 border-bottom mb-2" role="region" aria-label="Tag Details">
                        <strong>Tag Details:</strong> 
                        <span id="tag-info" class="ms-1">Select tags to see details</span>
                    </div>

                    <!-- Editor header buttons -->
                            <div class="editor-header mb-2 d-flex gap-2" role="toolbar" aria-label="Editor actions">
                            <button id="copy-doc-btn" class="btn btn-sm btn-secondary">
                            <i class="fas fa-copy me-1"></i> Copy Document
                            </button>
                            <button id="copy-tag-btn" class="btn btn-sm btn-secondary">
                            <i class="fas fa-tags me-1"></i> Copy Tag(s)
                            </button>
                            <button id="copy-address-btn" class="btn btn-sm btn-secondary">
                            <i class="fas fa-road me-1"></i> File Address
                            </button>
                            <button id="copy-path-btn" class="btn btn-sm btn-secondary">
                            <i class="fas fa-road me-1"></i> Copy Path
                            </button>
                    </div>

                    <!-- Code editor body -->
                    <div class="editor-body d-flex flex-grow-1 overflow-auto border rounded bg-dark" style="height:400px;">
                        <!-- Line numbers panel -->
                        <div id="line-numbers" 
                             class="line-numbers bg-secondary text-light text-end pe-2" 
                             style="user-select:none; flex-shrink:0; padding: 0.5rem;" 
                             aria-hidden="true"></div>

                        <!-- Code content panel -->
                        <pre id="code-content-area" 
                             class="code-content flex-grow-1 ps-2 text-light mb-0" 
                             contenteditable="true" 
                             spellcheck="false"
                             style="padding: 0.5rem; overflow: auto;"></pre>
                    </div>
                </div>`;

            // Setup elements
            const lineNumbersEl = document.getElementById('line-numbers');
            const codeContentEl = document.getElementById('code-content-area');
            const tagInfoEl = document.getElementById('tag-info');

            // Render line numbers
            const lines = file.lines || file.Lines || content.split(/\r?\n/);
            lineNumbersEl.innerHTML = lines.map((_, i) => `<div>${i + 1}</div>`).join('');

            // Apply syntax highlighting
            if (ext === 'json') {
                codeContentEl.innerHTML = this.syntaxHighlightJSON(content);
            } else if (ext === 'xml') {
                codeContentEl.innerHTML = this.syntaxHighlightXML(content);
            } else {
                codeContentEl.textContent = content;
            }

            // Setup scroll synchronization
            codeContentEl.addEventListener('scroll', () => {
                lineNumbersEl.scrollTop = codeContentEl.scrollTop;
            });

            // Setup copy buttons
            this.setupCopyButtons(file, content, filePath);
            this.setupTagSelection(file, codeContentEl, tagInfoEl);
        }

        // Setup copy button functionality
        //setupCopyButtons(file, content, filePath) {
        //    const copyDocBtn = document.getElementById('copy-doc-btn');
        //    const copyPathBtn = document.getElementById('copy-path-btn');
        //    const copyTagBtn = document.getElementById('copy-tag-btn');

        //    if (copyDocBtn) {
        //        copyDocBtn.onclick = () => this.copyToClipboard(content, 'Document content');
        //    }

        //    if (copyPathBtn) {
        //        copyPathBtn.onclick = () => this.copyToClipboard(filePath, 'File path');
        //    }
        //    // ✅ Copy only selected text (works for XML/JSON or anything else)
        //    copyTagBtn.addEventListener("click", () => {


        //        let selection = window.getSelection();
        //        let selectedText = selection.toString();

        //        var type = "text";
        //        if (!selectedText) {
        //            alert("Please select some text in the editor!");
        //            return;
        //        }

        //        try {
        //            if (!navigator.clipboard || !navigator.clipboard.writeText) {
        //                throw new Error('Clipboard API not supported');
        //            }
        //            navigator.clipboard.writeText(selectedText);
        //            alert(`${type} copied to clipboard!`);
        //        } catch (err) {
        //            console.error(`Could not copy ${type}: `, err);
        //            alert(`Failed to copy ${type}.`);
        //        }
        //    });

        //}
        setupCopyButtons(file, content, filePath) {
            const copyDocBtn = document.getElementById('copy-doc-btn');
            const copyTagBtn = document.getElementById('copy-tag-btn');
            const copyAddressBtn = document.getElementById('copy-address-btn');
            const copyPathBtn = document.getElementById('copy-path-btn');

            // Copy full document
            if (copyDocBtn) {
                copyDocBtn.onclick = () => this.copyToClipboard(content, 'Document content');
            }

            // Copy file address
            if (copyAddressBtn) {
                copyAddressBtn.onclick = () => this.copyToClipboard(filePath, 'File Address');
            }

            // Copy selected text (tag content)
            if (copyTagBtn) {
                copyTagBtn.addEventListener("click", () => {
                    const selection = window.getSelection();
                    const selectedText = selection.toString().trim();

                    if (!selectedText) {
                        alert("Please select some text in the editor!");
                        return;
                    }

                    try {
                        navigator.clipboard.writeText(selectedText);
                        alert("Selected text copied to clipboard!");
                    } catch (err) {
                        console.error("Could not copy text: ", err);
                        alert("Failed to copy text.");
                    }
                });
            }
            if (copyPathBtn) {
                copyPathBtn.onclick = () => {
                    const selection = window.getSelection();
                    const selectedText = selection.toString().trim();
                    let pathsToCopy = [];

                    // Parse XML content
                    const parser = new DOMParser();
                    let xmlDoc = null;
                    try {
                        xmlDoc = parser.parseFromString(content, "application/xml");

                        // Check for parsing errors
                        const parserError = xmlDoc.querySelector('parsererror');
                        if (parserError) {
                            throw new Error('XML parsing failed');
                        }
                    } catch {
                        alert("Content is not valid XML!");
                        return;
                    }

                    if (selectedText) {
                        // Find elements containing the selected text by traversing DOM
                        const foundElements = [];

                        function searchInElement(element) {
                            // Check if this element's direct text content contains the selected text
                            if (element.nodeType === 1) { // Element node
                                const elementText = element.textContent || "";
                                if (elementText.includes(selectedText)) {
                                    // Check if this is the most specific element containing the text
                                    let isLeafMatch = true;
                                    for (let child of element.children) {
                                        if (child.textContent && child.textContent.includes(selectedText)) {
                                            isLeafMatch = false;
                                            break;
                                        }
                                    }
                                    if (isLeafMatch) {
                                        foundElements.push(element);
                                    }
                                }

                                // Continue searching in child elements
                                for (let child of element.children) {
                                    searchInElement(child);
                                }
                            }
                        }

                        searchInElement(xmlDoc.documentElement);

                        for (let element of foundElements) {
                            const rootPath = getXMLRootPath(element);
                            if (rootPath && !pathsToCopy.includes(rootPath)) {
                                pathsToCopy.push(rootPath);
                            }
                        }

                        if (pathsToCopy.length === 0) {
                            alert("No XML elements found containing the selected text!");
                            return;
                        }
                    } else {
                        // No selection - get complete XML root path for all books
                        const books = xmlDoc.getElementsByTagName("book");
                        let completeXMLPath = "/CATALOG";

                        for (let i = 0; i < books.length; i++) {
                            const book = books[i];
                            let bookPath = "";

                            // Add book with ID or index
                            if (book.hasAttribute("id")) {
                                bookPath += `/BOOK[@ID='${book.getAttribute("id").toUpperCase()}']`;
                            } else {
                                bookPath += `/BOOK[${i}]`;
                            }

                            // Get all child elements of the book
                            const children = book.children;
                            for (let j = 0; j < children.length; j++) {
                                const child = children[j];
                                bookPath += `/${child.nodeName.toUpperCase()}`;
                            }

                            // Add book index separator
                            completeXMLPath += bookPath + `[${i}]`;
                        }

                        pathsToCopy.push(completeXMLPath);

                        if (pathsToCopy.length === 0) {
                            alert("No book elements found in XML!");
                            return;
                        }
                    }

                    const textToCopy = pathsToCopy.join("\n");
                    navigator.clipboard.writeText(textToCopy);
                    alert("Copied XML root paths:\n" + textToCopy);
                };
            }


            // --- Helper for XML XPath ---
            function getXMLXPath(node, index = null) {
                if (!node || node.nodeType !== 1) return "";
                let path = "";
                while (node && node.nodeType === 1) {
                    let name = node.nodeName;
                    if (node.hasAttribute("id")) {
                        name += `[@id='${node.getAttribute("id")}']`;
                    }
                    path = "/" + name + path;
                    node = node.parentNode;
                }
                if (index !== null) path += `[${index}]`;
                return path;
            }
        }


        // Setup tag selection functionality
        setupTagSelection(file, codeContentEl, tagInfoEl) {
            this.selectedTags = []; // Reset selection

            const selectableElements = codeContentEl.querySelectorAll('.tag, .json-key, .json-value');
            selectableElements.forEach((el, index) => {
                const type = el.classList.contains('json-key') ? 'Key' :
                    el.classList.contains('json-value') ? 'Value' : 'Tag';

                // Add tooltip
                el.setAttribute('data-bs-toggle', 'tooltip');
                el.setAttribute('title', `Type: ${type}\nContent: ${el.textContent}\nPath: ${file.fullPath || file.FullPath}`);

                // Initialize Bootstrap tooltip if available
                if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
                    new bootstrap.Tooltip(el, { html: true });
                }

                el.addEventListener('click', () => this.handleTagSelection(el, type, index + 1, file, tagInfoEl));
            });
        }


        // Handle tag selection
        handleTagSelection(element, tagOrKey, lineNumber, file, tagInfoEl) {
            const tagData = {
                tag: tagOrKey,
                metadata: `Line ${lineNumber}`,
                content: element.textContent,
                filePath: file.fullPath || file.FullPath
            };

            // Toggle selection
            const index = this.selectedTags.findIndex(t =>
                t.content === tagData.content && t.tag === tagData.tag
            );

            if (index >= 0) {
                this.selectedTags.splice(index, 1);
                element.classList.remove('selected-tag');
            } else {
                this.selectedTags.push(tagData);
                element.classList.add('selected-tag');
            }

            // Update info panel
            this.updateTagInfo(tagInfoEl);
        }

        // Update tag info display
        updateTagInfo(tagInfoEl) {
            if (!tagInfoEl) return;

            if (this.selectedTags.length === 0) {
                tagInfoEl.textContent = "Select tags to see details";
            } else if (this.selectedTags.length === 1) {
                const t = this.selectedTags[0];
                tagInfoEl.innerHTML = `
                    <strong>Tag:</strong> ${this.sanitizeHTML(t.tag)} 
                    <strong>Metadata:</strong> ${this.sanitizeHTML(t.metadata)} 
                    <strong>Path:</strong> ${this.sanitizeHTML(t.filePath)} 
                    <strong>Content:</strong> ${this.sanitizeHTML(t.content)}`;
            } else {
                tagInfoEl.innerHTML = `<strong>${this.selectedTags.length} items selected</strong>`;
            }
        }

        // JSON syntax highlighting
        syntaxHighlightJSON(jsonText) {
            if (!jsonText) return '';

            let json;
            try {
                json = JSON.parse(jsonText);
            } catch {
                return `<pre>${this.sanitizeHTML(jsonText)}</pre>`;
            }

            const traverse = (obj, indent = 0) => {
                const spacing = '  '.repeat(indent);
                if (obj === null) return `<span class="json-value">null</span>`;
                if (typeof obj === 'boolean' || typeof obj === 'number') {
                    return `<span class="json-value">${obj}</span>`;
                }
                if (typeof obj === 'string') {
                    return `<span class="json-value">"${this.sanitizeHTML(obj)}"</span>`;
                }

                if (Array.isArray(obj)) {
                    if (obj.length === 0) return '[]';
                    return '[<br>' + obj.map(item =>
                        spacing + '  ' + traverse(item, indent + 1)
                    ).join(',<br>') + `<br>${spacing}]`;
                }

                const keys = Object.keys(obj);
                if (keys.length === 0) return '{}';

                return '{<br>' + keys.map(k => {
                    const keyHtml = `<span class="json-key" data-key="${this.sanitizeHTML(k)}">"${this.sanitizeHTML(k)}"</span>`;
                    const valueHtml = traverse(obj[k], indent + 1);
                    return spacing + '  ' + keyHtml + ': ' + valueHtml;
                }).join(',<br>') + `<br>${spacing}}`;
            };

            return traverse(json);
        }

        // XML syntax highlighting
        syntaxHighlightXML(text) {
            if (!text) return '';

            const escaped = this.sanitizeHTML(text);
            return escaped.replace(/&lt;(\/?)(\w+)([^&]*)&gt;/g, (match, slash, tagName, attrs) => {
                const formattedAttrs = attrs.replace(
                    /(\w+)=&quot;([^&]*)&quot;/g,
                    '<span class="attr-name">$1</span>=<span class="attr-value">"$2"</span>'
                );
                return `<span class="tag" data-tag-name="${tagName}" data-attrs="${attrs.trim()}">&lt;${slash}${tagName}${formattedAttrs}&gt;</span>`;
            });
        }

        // Enhanced file deletion with proper confirmation
        async deleteFile(path, fileName) {
            if (!this.isValidPath(path)) {
                this.showError('Invalid file path provided.');
                return;
            }

            const confirmed = confirm(`Are you sure you want to delete "${fileName}"?\n\nThis action cannot be undone.`);
            if (!confirmed) return;

            try {
                const response = await fetch(`/api/file/delete-file?path=${encodeURIComponent(path)}`, {
                    method: 'DELETE',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });

                if (!response.ok) {
                    const errorData = await response.json().catch(() => ({}));
                    throw new Error(errorData.message || `HTTP ${response.status}: ${response.statusText}`);
                }

                this.showSuccess('File deleted successfully.');

                // Refresh current folder view
                if (this.activeFolderNode?.dataset?.path) {
                    await this.loadFolderContents(this.activeFolderNode.dataset.path);
                }
            } catch (error) {
                console.error('Error deleting file:', error);
                this.showError(`Failed to delete file: ${error.message}`);
            }
        }

        // Format file size
        formatFileSize(bytes) {
            if (bytes === 0) return '0 B';
            const k = 1024;
            const sizes = ['B', 'KB', 'MB', 'GB'];
            const i = Math.floor(Math.log(bytes) / Math.log(k));
            return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
        }

        // Copy to clipboard helper
        async copyToClipboard(text, type) {
            try {
                if (!navigator.clipboard || !navigator.clipboard.writeText) {
                    throw new Error('Clipboard API not supported');
                }
                await navigator.clipboard.writeText(text);
                alert(`${type} copied to clipboard!`);
            } catch (err) {
                console.error(`Could not copy ${type}: `, err);
                alert(`Failed to copy ${type}.`);
            }
        }



        // Enhanced event binding with proper cleanup
        bindEvents() {
            const folderTreeView = document.getElementById('folder-tree-view');
            const mainContentArea = document.getElementById('main-content-area');

            if (folderTreeView) {
                folderTreeView.addEventListener('click', this.handleTreeClick.bind(this));
            }

            if (mainContentArea) {
                mainContentArea.addEventListener('click', this.handleContentClick.bind(this));
            }
        }

        // Centralized tree click handler
        async handleTreeClick(e) {
            const folderNode = e.target.closest('.folder-node');
            const fileNode = e.target.closest('.file-node');

            if (fileNode?.dataset?.path) {
                await this.loadFileForViewing(fileNode.dataset.path);
                return;
            }

            if (folderNode?.dataset?.path) {
                await this.handleFolderClick(folderNode);
            }
        }

        // Handle folder node clicks
        async handleFolderClick(folderNode) {
            const path = folderNode.dataset.path;

            // Update active folder
            if (this.activeFolderNode) {
                this.activeFolderNode.classList.remove('active');
            }
            folderNode.classList.add('active');
            this.activeFolderNode = folderNode;

            // Toggle folder expansion
            const childrenContainer = folderNode.nextElementSibling;
            if (childrenContainer?.classList.contains('children')) {
                childrenContainer.classList.toggle('expanded');
                const caret = folderNode.querySelector('.caret');
                if (caret) caret.classList.toggle('expanded');
            }

            // Load folder contents
            await this.loadFolderContents(path);

            // Push state to history
            history.pushState({ folderPath: path }, "", `?path=${encodeURIComponent(path)}`);
        }

        // Centralized content area click handler
        async handleContentClick(e) {
            const viewBtn = e.target.closest('.view-btn');
            const deleteBtn = e.target.closest('.delete-btn');

            if (viewBtn?.dataset?.path) {
                await this.loadFileForViewing(viewBtn.dataset.path);
            } else if (deleteBtn?.dataset?.path) {
                await this.deleteFile(deleteBtn.dataset.path, deleteBtn.dataset.name || 'Unknown file');
            }
        }

        // Cleanup method for proper resource management
        destroy() {
            this.loadingStates.clear();
            this.selectedTags = [];
            this.activeFolderNode = null;

            // Remove event listeners
            const folderTreeView = document.getElementById('folder-tree-view');
            const mainContentArea = document.getElementById('main-content-area');

            if (folderTreeView) {
                folderTreeView.removeEventListener('click', this.handleTreeClick);
            }
            if (mainContentArea) {
                mainContentArea.removeEventListener('click', this.handleContentClick);
            }
        }
    }

    // Expose FileExplorer to window
    window.FileExplorer = FileExplorer;

    window.addEventListener("popstate", async (event) => {
        if (event.state?.folderPath) {
            // Render folder from history
            await window.fileExplorer.loadFolderContents(event.state.folderPath);

            // Restore active folder in tree
            const folderNodes = document.querySelectorAll('.folder-node');
            folderNodes.forEach(fn => fn.classList.remove('active'));
            const activeNode = Array.from(folderNodes).find(fn => fn.dataset.path === event.state.folderPath);
            if (activeNode) window.fileExplorer.activeFolderNode = activeNode;
            if (activeNode) activeNode.classList.add('active');
        }
    });


    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', async () => {
        if (!window.fileExplorer) window.fileExplorer = new FileExplorer();

        const params = new URLSearchParams(window.location.search);
        const folderPath = params.get("path") || ""; // default root folder
        if (folderPath) {
            await window.fileExplorer.loadFolderContents(folderPath);
            history.replaceState({ folderPath }, "", `?path=${encodeURIComponent(folderPath)}`);
        }
    });


    // Cleanup on page unload
    window.addEventListener('beforeunload', () => {
        if (window.fileExplorer) {
            window.fileExplorer.destroy();
        }
    });

})();
