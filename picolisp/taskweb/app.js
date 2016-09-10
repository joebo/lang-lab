/*global m */

var session = {}

var serializer = function(data) {
    //console.log(data);
    var postData = '*Data=' + encodeURIComponent(JSON.stringify(data));
    if (session.key) {
        postData = postData + '&*Session=' + session.key;
    }
    return postData;
};


var todo = function() {
     var Todo = function(data) {
         this.desc = m.prop(data.desc);
         this.done = m.prop(false);
     };

    //could use vm in the future if needed
    var vm = {
        list: m.prop([]),
        description: m.prop("")
    };
    this.vm = vm;
    
    this.controller = function() {
        var self = this;
        this.refresh = function() {
            return m.request({method: "GET", url: "/!todo-list-json"}).then(function(json) {
                vm.list(json.todos.map(function(x) { return new Todo(x) }));
            });
        }

        this.add = function() {
            if (vm.description()) {
                return m.request({method: "POST", url: "/!todo-add-json", data: {desc: vm.description(), blah: 1}, serialize: serializer})
                    .then(function() {
                        vm.description("");
                        self.refresh();
                    });
            }
        };
        
        this.refresh();
    };


    this.view = function(c) {
        return [
            m("input", {onchange: m.withAttr("value", vm.description), value: vm.description()}),
            m("button", {onclick: c.add}, "Add"),
            m("table", [
                vm.list().map(function(task, index) {
                    return m("tr", [
                        m("td", [
                            m("input[type=checkbox]", {onclick: m.withAttr("checked", task.done), checked: task.done()})
                        ]),
                        m("td", {style: {textDecoration: task.done() ? "line-through" : "none"}}, task.desc())
                    ]);
                })
            ])
        ];
    };
}


var login = function() {
    var msg = m.prop("Welcome, please log in below.");


    var vm = {
        username : m.prop(""),
        password : m.prop("")
    };

    this.vm = vm;
    this.msg = msg;

    this.controller = function() {
        this.login = function() {
            return m.request({method: "POST", url: "/!user-auth", data: vm, serialize: serializer}).then(function(json) {
                if (json.session) {
                    session.key = json.session;
                    m.route("/todo");
                }
                else {
                    msg("invalid username / password");
                    //console.log(msg());
                }
            });
        };
    };

    this.view = function(c) {
        return [
            m("div", msg()),
            m("label", "username"),
            m("input", { onchange: m.withAttr("value", vm.username) }),
            m("label", "password"),
            m("input[type='password']", { onchange: m.withAttr("value", vm.password) }),
            m("button", { onclick: c.login }, "Log In")
        ];
    };

}


//initialize the application
//m.mount(document.body, {controller: todo.controller, view: todo.view});



