
NB. no stdlib yet
cut=: ' '&$: :([: -.&a: <;._2@,~)

dispatch =: 3 : 0
if. url_request_ -: '/form/' do. formController ''
elseif. url_request_ -: '/sleep/' do. sleepController ''
elseif. url_request_ -: '/source/' do. sourceController ''
elseif. url_request_ -: '/big/' do. renderLayout 1e6 $ 'foo'
elseif. url_request_ -: '/google/' do. googleController ''
elseif. do. defaultController ''
end.
)

IndexHtml =: 0 : 0
<h1>Menu</h1>
<a href="/form/">Form Example</a><br>
<a href="/sleep/">Sleep Example (Will take 5 seconds to complete, but you can do other things in other tabs)</a><br>
<a href="/source/">View Page Source</a>
<br><br>
<a href="https://github.com/joebo/lang-lab/tree/master/go/http-j">github</a><br><br><br>
)

renderLayout =: 3 : 0
response_request_ =: y,IndexHtml
)

htmlEncode =: 3 : 0
('<';'&lt;';'>';'&gt;') stringreplace y
)

loadProfile =: 3 : 0
BINPATH_z_=: 'C:\Users\Joe Bogner\j64-802\bin'
ARGV_z_=:''
(3 : '0!:0 y')<BINPATH,'\profile.ijs'
)

googleController =: 3 : 0
loadProfile''
require 'task'
google=.shell'curl --silent http://www.google.com'

renderLayout '<textarea style="width:500px;height:300px">',(htmlEncode google),'</textarea>'
)

sourceController =: 3 : 0
loadProfile''
page=. 1!:1 <'server.ijs'
renderLayout '<pre>',(htmlEncode page),'<pre>'
)

sleepController =: 3 : 0
6!:3 (5)
renderLayout 'zzzz, wouldn''t it be nice if we can redirect?(not yet)'
)

FormHtml =: 0 : 0
<form action="/form/" method="POST">
Name: <input type="text" name="name">
<input type="submit">
</form>
)

formController =: 3 : 0
Greeting=. ''
posted=. '=' cut body_request_
if. (#posted)>1 do. Greeting=. 'Hello ', 1{::posted end.
renderLayout (Greeting,FormHtml)
)


defaultController =: 3 : 0
response_request_ =: IndexHtml, 'You requested: ', url_request_, ('<br> body: ', body_request_)
)



dispatch ''
