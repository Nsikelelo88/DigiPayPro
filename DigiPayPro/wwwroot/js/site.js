$(document).ready(function () {
    var table = $('#employeesTable');

    // Check if DataTable is already initialized
    if ($.fn.DataTable.isDataTable(table)) {
        // Get the existing DataTable instance
        var dataTable = table.DataTable();
        // Destroy it completely
        dataTable.destroy();
        // Remove any DataTables-added classes
        table.removeAttr('style');
    }

    // Initialize with proper options
    table.DataTable({
        responsive: true,
        ordering: true,
        searching: true,
        pageLength: 10,
        // These options help prevent initialization issues
        destroy: true,
        retrieve: true,
        autoWidth: false
    });
});5