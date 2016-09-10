var jsdom = require("jsdom");
var request = require("request");
var expect = require('chai').expect;

var URL = "http://localhost:8080/index.html";

describe('Todo', function() {
    var self = this;

    before(function(done) {
        var virtualConsole = jsdom.createVirtualConsole().sendTo(console);
        virtualConsole.on("log", function (message) {
            console.log("console.log called ->", message);
        });

        jsdom.env({
            url: URL,
            resourceLoader: function (resource, callback) {
                //return resource.defaultFetch(callback);
                //using a new request object instead of the jsdom one... errors with ECONRESET otherwise
                request({uri: resource.url.href}, function(error, response, body) {
                    callback(null, body);
                });

            },
            virtualConsole: virtualConsole,
            features: {
                FetchExternalResources: ["script"],
                ProcessExternalResources: ["script"],
                SkipExternalResources: false
            },
            created: function(error, win) {
                //stub to prevent error
                win.scrollTo = function() {};
            },
            done: function (err, win) {
                if (err) { console.log(err) }
                self.window = win;
                global.win = win;
                done();
            },
            agentOptions: { keepAlive: false, keepAliveMsecs: 0 }
            //pool: null
            //proxy: "http://192.168.2.36:8888"
        });
    });

    it('should not log in on bad username/password', function(done) {
        win.document.location.hash = '#/';
        var l = new win.login();
        l.vm.username("admin");
        l.vm.password("BADPASS");
        var controller = new l.controller();
        controller.login().then(function(json) {
            setTimeout(function() {
                expect(l.msg()).to.have.string('invalid');
                expect(win.document.location.hash).to.be.equal('#/');
                done();
            },10);
            
        });
    });

    it('should log in', function(done) {
        win.document.location.hash = '#/';
        var l = new win.login();
        l.vm.username("admin");
        l.vm.password("admin");
        var controller = new l.controller();
        controller.login().then(function(json) {
            //console.log(win.session.key);
            setTimeout(function() {
                expect(win.document.location.hash).to.be.equal('#/todo');
                done();
            },10);
            
        });
    });
    

    it('should fetch todos with session key', function(done) {
        //console.log(win.session.key);
        var todo = new win.todo();
        var ctrl = new todo.controller();
        ctrl.refresh().then(function(json) {
            expect(todo.vm.list().length).to.be.above(0);
            done();
        });
    });

    it('adds a todo with session key', function(done) {
        //console.log(win.session.key);
        var todo = new win.todo();
        var ctrl = new todo.controller();
        todo.vm.description('test #5');
        ctrl.add().then(function() {
            ctrl.refresh().then(function() {
                var itemsJSON = JSON.stringify(todo.vm.list());
                expect(itemsJSON).to.have.string('test #5');
                done();
            });

        });
    });

});

