(function () {

    var PromiseSource = this.PromiseSource = new Class({
        initialize: function () {
            var result = undefined;
            var finnished = false;
            var waiters = [];

            this.finalize = function (res) {
                if (finnished) return;
                result = res;
                finnished = true;
                waiters.each(function (waiter) {
                    waiter(res);
                });
            };

            this.getPromise = function () {

                return {
                    continueWith: function (fn) {
                        var p = new PromiseSource();
                        var finFun = function () {
                            p.finalize(fn(result));
                        };
                        if (finnished) {
                            finFun;
                        } else {
                            waiters.push(finFun);
                        }
                        return p.getPromise();
                    }
                };

            };
        }
    });

})();