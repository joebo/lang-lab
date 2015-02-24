
NB. no stdlib yet
cut=: ' '&$: :([: -.&a: <;._2@,~)

dispatch =: 3 : 0
if. url_request_ -: '/form/' do. formController ''
elseif. url_request_ -: '/sleep/' do. sleepController ''
elseif. url_request_ -: '/source/' do. sourceController ''
elseif. do. defaultController ''
end.
)

renderLayout =: 3 : 0
response_request_ =: y,IndexHtml
)

sourceController =: 3 : 0
page=. 1!:1 <'server.ijs'

renderLayout '<pre>',page,'<pre>'
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
posted=: '=' cut body_request_
Greeting=. ''
if. (#posted)>1 do. Greeting=. 'Hello ', 1{::posted end.
renderLayout Greeting,FormHtml
)

IndexHtml =: 0 : 0
<h1>Menu</h1>
<a href="/form/">Form Example</a><br>
<a href="/sleep/">Sleep Example (Will take 5 seconds to complete, but you can do other things in other tabs)</a><br>
<a href="/source/">View Page Source</a>
<br><br>
<a href="https://github.com/joebo/lang-lab/tree/master/go/http-j">github</a><br><br>
)

defaultController =: 3 : 0
response_request_ =: IndexHtml, 'You requested: ', url_request_, ('<br> body: ', body_request_)
)



dispatch ''
