var app = angular.module("myApp", []);

app.controller("myController", function ($scope, $http) {
    $scope.showEnglish = true; // Default to English

    $scope.toggleLanguage = function () {
        $scope.showEnglish = !$scope.showEnglish;
    };

    $scope.relativeDetails = [{}];
    $scope.companyDetails = [{}];
    $scope.errorMessage = "";
    $scope.successMessage = "";

    // Add Row Functionality
    $scope.addRow = function (table) {
        table.push({});
    };

    // Remove Row Functionality
    $scope.removeRow = function (table, index) {
        table.splice(index, 1);
    };

    // Submit Form
    $scope.submitForm = function () {
        const formData = {
            Name: $scope.formData.name,
            StaffId: $scope.formData.StaffId,
            Position: $scope.formData.position,
            Subsidiaries: $scope.formData.subsidiaries,
            relativeDetails: $scope.relativeDetails,
            companyDetails: $scope.companyDetails
        };
        console.log("Submitting form with data:", formData);

        $http.post("/api/records", formData)
            .then(function (response) {

                console.log("Response received:", response.data);
                if (response.data.message === "Duplicate record found for this Staff ID in the current year.") {
                    $scope.errorMessage = "Record already exists with the given Staff ID in the current year.";
                    $scope.successMessage = "";
                } else if (response.data.message === "Record and associated data inserted successfully.") {
                    $scope.successMessage = "Record added successfully!";
                    $scope.errorMessage = "";
                }
            })
            .catch(function (error) {
                console.error("Error occurred:", error);
                $scope.errorMessage = "An error occurred while processing the request.";
                $scope.successMessage = "";
            });
    };
});