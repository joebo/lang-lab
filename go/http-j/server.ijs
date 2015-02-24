
NB. no stdlib yet
cut=: ' '&$: :([: -.&a: <;._2@,~)
LF=:10{::a.
DELIM=:30{::a.

dispatch =: 3 : 0
if. url_request_ -: '/form/' do. formController ''
elseif. url_request_ -: '/sleep/' do. sleepController ''
elseif. url_request_ -: '/source/' do. sourceController ''
elseif. url_request_ -: '/big/' do. renderLayout 1e6 $ 'foo'
elseif. url_request_ -: '/google/' do. googleController ''
elseif. url_request_ -: '/redirect/' do. redirectController ''
elseif. url_request_ -: '/cookie/' do. cookieController ''
elseif. url_request_ -: '/cookieAfter/' do. cookieAfterController ''
elseif. do. defaultController ''
end.
)

NB. todo
getPosted =: 3 : 0
posted=. '=' cut body_request_
Value=.''
if. (#posted)>1 do. Value=.1{::posted end.
Value
)

IndexHtml =: 0 : 0
<h1>Menu</h1>
<a href="/form/">Form Example</a><br>
<a href="/cookie/">Cookie Example</a><br>
<a href="/redirect/">Redirect Example</a><br>
<a href="/sleep/">Sleep Example (Will take 5 seconds to complete, but you can do other things in other tabs)</a><br>
<a href="/source/">View Page Source</a>
<br><br>
<a href="https://github.com/joebo/lang-lab/tree/master/go/http-j">github</a><br><br><br>
)

renderLayout =: 3 : 0
response_request_ =: ('Content-Type: text/html',LF,LF),(y,IndexHtml)
)

redirectController =: 3 : 0
response_request_ =: ('Location: /',LF,LF)
)


cookieController =: 3 : 0
response_request_ =: ('Location: /cookieAfter/',LF,'Set-Cookie: token=abc; path=/',LF,LF)
)

cookieAfterController =: 3 : 0
response_request_ =: renderLayout '<span id="Cookie"></span><script>document.getElementById("Cookie").innerText=''cookie from script: '' + document.cookie</script>'
)


htmlEncode =: 3 : 0
('<';'&lt;';'>';'&gt;') stringreplace y
)

loadProfile =: 3 : 0
cwd=.1!:43''
0!:0 <(cwd,'/environment.ijs')
ARGV_z_=:''
(3 : '0!:0 y')<BINPATH,'/profile.ijs'
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
renderLayout '<pre>',(htmlEncode page),'</pre>'
)

sleepController =: 3 : 0
6!:3 (5)
renderLayout 'zzzz'
)

FormHtml =: 0 : 0
<form action="/form/" method="POST">
Name: <input type="text" name="name">
<input type="submit">
</form>
)

formController =: 3 : 0
Greeting=. ''
Name=. getPosted 'name'
if. (#Name)>1 do. Greeting=. 'Hello ', Name end.
renderLayout (Greeting,FormHtml)
)


defaultController =: 3 : 0
response_request_ =: IndexHtml, 'You requested: ', url_request_, ('<br> body: ', body_request_)
)



dispatch ''
