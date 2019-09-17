﻿(function () {
    'use strict';

    angular.module('ascApp').controller('ViewBlueprintAssignmentDetailsCtrl', ViewBlueprintAssignmentDetailsCtrl);
    ViewBlueprintAssignmentDetailsCtrl.$inject = ['$state', '$uibModalInstance', 'initialData', 'subscriptionId','subscriptionName', 'ascApi', 'toastr'];

    /* @ngInject */
    function ViewBlueprintAssignmentDetailsCtrl($state, $uibModalInstance, initialData, subscriptionId, subscriptionName, ascApi, toastr) {
        /* jshint validthis: true */
        var vm = this;
        vm.subscriptionId = subscriptionId;
        vm.subscriptionName = subscriptionName;
        vm.blueprintAssignment = initialData;
        vm.close = close;
        vm.onUpdateButtonClick = onUpdateButtonClick;
        vm.createdDate = getFormattedDate(vm.blueprintAssignment.createdDate);
        vm.lastModifiedDate = getFormattedDate(vm.blueprintAssignment.lastModifiedDate);

        function close() {
            $uibModalInstance.dismiss();
        }

        function onUpdateButtonClick() {
            $uibModalInstance.dismiss();
            $state.go('update-blueprint-assignment', {
                subscriptionId: vm.subscriptionId,
                subscriptionName: vm.subscriptionName,
                blueprintAssignmentName: vm.blueprintAssignment.name
            });
        }

        function getFormattedDate(dateToBeFormatted) {
            var date = new Date(dateToBeFormatted);
            var formattedDate = date.toLocaleString();
            return formattedDate;
        }
    }
})();