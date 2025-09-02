//document.addEventListener('DOMContentLoaded', function () {
//    const treeView = document.getElementById('tree-view');
//    const fileView = document.getElementById('file-view');
//    const filePathElement = document.getElementById('file-path');
//    const fileContentView = document.getElementById('file-content-view');
//    const viewerHeader = document.getElementById('viewer-header');
//    const copyTagBtn = document.getElementById('copy-tag-btn');
//    const copyPathBtn = document.getElementById('copy-path-btn');

//    let fileDataStore = [];
//    let activeFileElement = null;

//    // --- Data Fetching (Simulating AJAX call to .NET Controller) ---
//    async function fetchFileStructure() {
//        // In a real .NET app, this would be: const response = await fetch('/File/GetFileStructure');
//        // For this standalone demo, we simulate the response with local data.
//        const fileStructureJson = `[
//                    {
//                        "Name": "Logs", "Type": "folder", "Path": "Logs",
//                        "Children": [
//                            { "Name": "log-2023-10-26.xml", "Type": "xml", "Path": "Logs/log-2023-10-26.xml", "Content": "<log>\\n  <entry>\\n    <timestamp>2023-10-26T10:00:00Z</timestamp>\\n    <message>Application started successfully.</message>\\n  </entry>\\n</log>" },
//                            { "Name": "errors.json", "Type": "json", "Path": "Logs/errors.json", "Content": "{\\n  \\"error\\": {\\n    \\"code\\": 500,\\n    \\"message\\": \\"Internal Server Error: Could not connect to database.\\"\\n  }\\n}" }
//                        ]
//                    },
//                    {
//                        "Name": "Documents", "Type": "folder", "Path": "Documents",
//                        "Children": [
//                            { "Name": "project-brief.doc", "Type": "doc", "Path": "Documents/project-brief.doc", "Content": "Project Brief\\n=============\\nThis document outlines the requirements for the new file explorer application." },
//                            {
//                                "Name": "Archive", "Type": "folder", "Path": "Documents/Archive",
//                                "Children": [
//                                    { "Name": "old-spec.json", "Type": "json", "Path": "Documents/Archive/old-spec.json", "Content": "{\\n  \\"version\\": \\"1.0\\",\\n  \\"deprecated\\": true\\n}" }
//                                ]
//                            }
//                        ]
//                    },
//                    { "Name": "README.md", "Type": "md", "Path": "README.md", "Content": "# File Explorer\\nThis is a web-based file explorer." },
//                    { "Name": "config.xml", "Type": "xml", "Path": "config.xml", "Content": "<config>\\n  <setting name=\\"timeout\\" value=\\"30\\" />\\n  <setting name=\\"retries\\" value=\\"3\\" />\\n</config>" }
//                ]`;

//        try {
//            fileDataStore = JSON.parse(fileStructureJson);
//            renderTreeView(fileDataStore, treeView);
//        } catch (error) {
//            console.error('Error fetching or parsing file structure:', error);
//            treeView.innerHTML = '<p class="text-red-500">Failed to load file structure.</p>';
//        }
//    }

//    // --- UI Rendering ---
//    function renderTreeView(items, container) {
//        container.innerHTML = ''; // Clear previous content
//        items.forEach(item => {
//            const treeItem = createTreeItem(item);
//            container.appendChild(treeItem);
//            if (item.Type === 'folder' && item.Children && item.Children.length > 0) {
//                const childrenContainer = document.createElement('div');
//                childrenContainer.className = 'children hidden';
//                container.appendChild(childrenContainer);
//                renderTreeView(item.Children, childrenContainer);
//            }
//        });
//    }

//    function getFileIcon(type) {
//        switch (type.toLowerCase()) {
//            case 'folder': return 'fa-folder';
//            case 'json': return 'fa-file-code';
//            case 'xml': return 'fa-file-code';
//            case 'doc':
//            case 'docx': return 'fa-file-word';
//            case 'md': return 'fa-file-alt';
//            default: return 'fa-file';
//        }
//    }

//    function createTreeItem(item) {
//        const itemElement = document.createElement('div');
//        itemElement.className = 'tree-item';
//        itemElement.dataset.path = item.Path;

//        const icon = document.createElement('i');
//        const iconClass = getFileIcon(item.Type);
//        icon.className = `fas ${iconClass} ${item.Type === 'folder' ? 'text-yellow-500' : 'text-gray-500'}`;

//        const nameSpan = document.createElement('span');
//        nameSpan.textContent = item.Name;

//        itemElement.appendChild(icon);
//        itemElement.appendChild(nameSpan);

//        itemElement.addEventListener('click', (e) => {
//            e.stopPropagation();
//            handleTreeItemClick(item, itemElement);
//        });

//        return itemElement;
//    }

//    // --- Event Handlers ---
//    function handleTreeItemClick(item, element) {
//        if (item.Type === 'folder') {
//            const childrenContainer = element.nextElementSibling;
//            if (childrenContainer && childrenContainer.classList.contains('children')) {
//                childrenContainer.classList.toggle('hidden');
//                const icon = element.querySelector('i');
//                icon.classList.toggle('fa-folder');
//                icon.classList.toggle('fa-folder-open');
//            }
//        } else {
//            // It's a file
//            if (activeFileElement) {
//                activeFileElement.classList.remove('active');
//            }
//            element.classList.add('active');
//            activeFileElement = element;

//            displayFileContent(item);
//        }
//    }

//    function displayFileContent(fileItem) {
//        filePathElement.textContent = fileItem.Path;
//        fileContentView.textContent = fileItem.Content || 'File is empty or content could not be loaded.';
//        viewerHeader.classList.remove('hidden');
//    }

//    // --- Clipboard Logic ---
//    copyTagBtn.addEventListener('click', () => {
//        const content = fileContentView.textContent;
//        const path = filePathElement.textContent;
//        const fileType = path.split('.').pop().toLowerCase();
//        let tagToCopy = '';

//        if (fileType === 'xml' || fileType === 'json') {
//            // Simple logic to grab the first "tag-like" structure.
//            const match = content.match(/<(\w+)|"(\w+)":/);
//            if (match) {
//                tagToCopy = match[1] || match[2];
//            }
//        } else {
//            tagToCopy = 'N/A for this file type';
//        }

//        copyToClipboard(tagToCopy, 'Tag');
//    });

//    copyPathBtn.addEventListener('click', () => {
//        const path = filePathElement.textContent;
//        copyToClipboard(path, 'Path');
//    });

//    function copyToClipboard(text, type) {
//        // Using the modern Clipboard API
//        navigator.clipboard.writeText(text).then(() => {
//            showToast(`${type} copied to clipboard!`);
//        }).catch(err => {
//            console.error(`Could not copy ${type}: `, err);
//            // Fallback for older browsers or insecure contexts.
//            // The 'document.execCommand' method is deprecated but provides a useful fallback.
//            try {
//                const textArea = document.createElement("textarea");
//                textArea.value = text;
//                document.body.appendChild(textArea);
//                textArea.focus();
//                textArea.select();
//                document.execCommand('copy');
//                document.body.removeChild(textArea);
//                showToast(`${type} copied to clipboard!`);
//            } catch (fallbackErr) {
//                showToast(`Failed to copy ${type}.`, 'error');
//            }
//        });
//    }

//    function showToast(message, type = 'success') {
//        const toast = document.createElement('div');
//        toast.textContent = message;
//        toast.className = 'fixed bottom-5 right-5 p-4 rounded-lg shadow-lg text-white';
//        toast.className += type === 'success' ? ' bg-green-500' : ' bg-red-500';

//        document.body.appendChild(toast);

//        setTimeout(() => {
//            toast.remove();
//        }, 3000);
//    }

//    // --- Initial Load ---
//    fetchFileStructure();
//});