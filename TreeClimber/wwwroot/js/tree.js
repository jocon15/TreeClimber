/*TREE FUNCTIONS variable will store our functions instead of putting them in the window */
TREE_FUNCTIONS = {
    expandAll: function () {
        var x = document.getElementsByTagName("details");
        var i;
        for (i = 0; i < x.length; i++) {
            x[i].setAttribute("open", "true");
        }
    },

    collapseAll: function () {
        var x = document.getElementsByTagName("details");
        var i;
        for (i = 0; i < x.length; i++) {
            x[i].removeAttribute("open");
        }
    },

    downloadFileFromStream: async function (fileName, contentStreamReference) {
        const arrayBuffer = await contentStreamReference.arrayBuffer();
        const blob = new Blob([arrayBuffer]);
        const url = URL.createObjectURL(blob);
        const anchorElement = document.createElement('a');
        anchorElement.href = url;
        anchorElement.download = fileName ?? '';
        anchorElement.click();
        anchorElement.remove();
        URL.revokeObjectURL(url);
    },

    clipboardCopy: function (text) {
        navigator.clipboard.writeText(text).then(function () {
            // alert("Copied to clipboard!");
        })
            .catch(function (error) {
                alert(error);
            });
    }
}
