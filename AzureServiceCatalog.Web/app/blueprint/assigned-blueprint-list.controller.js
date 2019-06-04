﻿(function () {
    'use strict';

    angular.module('ascApp').controller('AssignedBlueprintListCtrl', AssignedBlueprintListCtrl);
    AssignedBlueprintListCtrl.$inject = ['initialData', 'ascApi'];

    /* @ngInject */
    function AssignedBlueprintListCtrl(initialData, ascApi) {
        /* jshint validthis: true */
        var vm = this;
        vm.assignedBlueprints = [];
        vm.selectedSubscription = null;
        vm.getBlueprintName = getBlueprintName;
        vm.getBlueprintVersion = getBlueprintVersion;
        vm.subscriptions = initialData;
        vm.subscriptionChanged = subscriptionChanged;

        activate();

        function activate() {
            if (vm.subscriptions && vm.subscriptions.length > 0) {
                vm.selectedSubscription = vm.subscriptions[0];
                subscriptionChanged();
            }
        }

        function getBlueprintName(assignedBlueprint) {
            var blueprintName = "";
            if (assignedBlueprint.properties["blueprintId"] !== undefined) {
                var tempArr = assignedBlueprint.properties["blueprintId"].split('/');
                var bluePrintsIndex = tempArr.indexOf('blueprints');
                blueprintName = tempArr[bluePrintsIndex+1];
            }
            return blueprintName;
        }

        function getBlueprintVersion(assignedBlueprint) {
            var version = "";
            if (assignedBlueprint.properties["blueprintId"] !== undefined) {
                var tempArr = assignedBlueprint.properties["blueprintId"].split('/');
                var versionsIndex = tempArr.indexOf('versions');
                version = tempArr[versionsIndex + 1];
            }
            return version;
        }

        function subscriptionChanged() {
            ascApi.getAssignedBlueprints(vm.selectedSubscription.rowKey).then(function (data) {
                vm.assignedBlueprints = data.value;
            });
        }
    }
})();