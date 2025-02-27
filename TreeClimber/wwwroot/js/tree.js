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
    }
}
