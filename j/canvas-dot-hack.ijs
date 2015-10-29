NB. load 'c:/joe/lang-lab/j/canvas-dot-hack.ijs'

require 'plot'

canvasdot_jzplot_=: 3 : 0
'v s f e c p'=. y
p=. citemize p
v=. v * CANVAS_PENSCALE

if. 3 = 4!:0 <'canvasdot_hover' do. canvasdot_hover '' end.

if. is1color e do.
  pbuf 1 canvas_color e
  pbuf 'pts.push([',"1 (0&pfmtjs flipxy p) ,"1 ',' ,"1 (0&pfmtjs v) ,"1,']);'
  pbuf 'ctx.beginPath();ctx.arc(' ,"1 (0&pfmtjs flipxy p) ,"1 ',' ,"1 (0&pfmtjs v) ,"1 ',0,2*Math.PI,1);ctx.fill();ctx.closePath();'
else.
  e=. p cmatch e
  for_c. p do.
    pbuf 1 canvas_color c_index { e
    pbuf 'ctx.beginPath();ctx.arc(' , (0&pfmtjs flipxy c) , ',' , (0&pfmtjs v) , ',0,2*Math.PI,1);ctx.fill();ctx.closePath();'
  end.
end.
)

canvas_build_jzplot_=: 4 : 0
if. #x do.
  'function(ctx){',LF,y,'}',LF
else.
  canvas_wrap (canvas_header''),LF,'function drawPlot(ctx,pts,text, hover) {',LF,y,LF,'}',LF
end.
)

canvas_template_jzplot_=: 0 : 0
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset=utf-8>
	<title>J Plot</title>
	<style>
		#canvas1 { margin-left:80px; margin-top:40px; }
	</style>

  <1>
    <script type="text/javascript">

                //http://stackoverflow.com/questions/11646398/cross-browser-innertext-for-setting-values
                function graph() {

                    var setText = (function() {
                           function setTextInnerText(element, msg) {
                                element.innerText = msg;
                            }

                            function setTextTextContent(element, msg) {
                                element.textContent = msg;
                            }

                            return "innerText" in document.createElement('span') ? setTextInnerText : setTextTextContent;
                        })();

                        function windowToCanvas(canvas, x, y) {
                            var bbox = canvas.getBoundingClientRect();

                            return { x: x - bbox.left * (canvas.width  / bbox.width),
                                     y: y - bbox.top  * (canvas.height / bbox.height)
                                   };
                        }

			var graphCanvas = document.getElementById('canvas1');
			// ensure that the element is available within the DOM
			if (graphCanvas && graphCanvas.getContext) {
			    // open a 2D context within the canvas
			    var context = graphCanvas.getContext('2d');
			    // draw
                            var text=[];
                            var pts=[];
                            var hover=[];

			    drawPlot(context, pts, text, hover);

                            //difference between the first x label and 2nd xlabel in plot units (e.g. 5.5 - 5)
                            var xSpace = parseFloat(text[0][2][0])-parseFloat(text[0][1][0]);
                            //difference between the first x label and 2nd xlabel in coordinates divided by the plot space that covers
                            var xSpacePixels = (text[0][3][1]-text[0][2][1]) / xSpace;
                            //first x label
                            var xStart = parseFloat(text[0][0][0]);
                            //distance from left for first x label
                            var xOffset = text[0][0][1];

                            //difference between the first y label and 2nd ylabel in plot units (e.g. 5.5 - 5)
                            var ySpace = parseFloat(text[1][2][0])-parseFloat(text[1][1][0]);
                            //difference between the first y label and 2nd ylabel in coordinates divided by the plot space that covers
                            var ySpacePixels = (text[1][3][2]-text[1][2][2]) / ySpace;
                            //first y label
                            var yStart = parseFloat(text[1][0][0]);
                            //distance from left for first y label
                            var yOffset = text[1][0][2];

                            var coordsLabel = document.getElementById('coordscanvas1');
                            graphCanvas.onmousemove = function (e) {
                                var xy = windowToCanvas(graphCanvas, e.clientX, e.clientY);
                                xy.graphX = ((xy.x-xOffset)/xSpacePixels) + xStart;
                                xy.graphY = ((xy.y-yOffset)/ySpacePixels) + yStart;

                                var coords = 'x: ' + Number(xy.graphX).toFixed(3) + ' y: ' + Number(xy.graphY).toFixed(3);

                                for(var i = 0; i < pts.length; i++) {
                                    var radius=pts[i][2];
                                    var dx = xy.x-pts[i][0];
                                    var dy = xy.y-pts[i][1];
                                    var isHit = (dx*dx+dy*dy)<(radius*radius);
                                    if (isHit) {
                                        //console.log(pts[i]);
                                        coords += ' HOVER: ' + hover[i];
                                    }
                                }
                                //console.log(xy);
                                setText(coordsLabel, coords);
                            }
			}
		}
)

CANVAS_TRL_jzplot_=: 0 : 0
	</script>
</head>
<body onLoad="graph();">
	<article>
		<h1>plot</h1>
                <span id="coordscanvas1"></span><br>
		<canvas id="canvas1" width="800" height="400"></canvas>
	</article>
</body>
</html>
)


canvas_text_jzplot_=: 3 : 0
'fnt txt pos align rot und'=. y
pos=. citemize pos
txt=. ,each boxxopen txt
txt=. utf8@('"'&,)@(,&'"')@jsesc each txt

if. 1=#txt do. txt=. (#pos)#{.txt end.

fn=. 'ctx.textBaseline= "middle";ctx.textAlign= "', (align{:: 'left';'center';'right'), '";'

pbuf 'var t = []'
pbuf tolist (<'t.push([') ,each txt, each (<','), each (<("1) 0&pfmtjs flipxy pos >. 0), each (<'])')
pbuf 'text.push(t);'

select. rot
case. 0 do.
  res=. tolist (<fn) ,each (<'ctx.fillText(') ,each txt ,each (<',') ,each (<("1) 0&pfmtjs flipxy pos >. 0) ,each <');'
case. 1 do.
  r=. ''
  for_i. i.#pos do.
    s=. 'ctx.save();ctx.translate', (pfmtjs flipxy 0 >. i{pos), ';ctx.rotate(-Math.PI/2);'
    r=. r, <s, fn, 'ctx.fillText(' , (i pick txt), ',0,0);ctx.restore();'
  end.
  res=. tolist r
case. 2 do.
  r=. ''
  for_i. i.#pos do.
    s=. 'ctx.save();ctx.translate', (pfmtjs flipxy 0 >. i{pos), ';ctx.rotate(Math.PI/2);'
    r=. r, <s, fn, 'ctx.fillText(' , (i pick txt), ',0,0);ctx.restore();'
  end.
  res=. tolist r
end.

if. -. und do. res return. end.
wid=. ,{. fnt pgetextent txt
'off lwd'=. getunderline fnt
res=. res, LF, 'ctx.lineWidth="', (":1>.lwd) ,'";'

select. rot
case. 0 do.
  bgn=. pos - (wid * -: align),.-off
  end=. bgn + wid,.0
case. 1 do.
  bgn=. pos - off,.wid * -: align
  end=. bgn + 0,.wid
case. 2 do.
  bgn=. pos + off,.wid * -: align
  end=. bgn - 0,.wid
end.

bgn=. 'ctx.beginPath();ctx.moveTo' ,"1 (pfmtjs flipxy bgn >. 0) ,"1 ';'
end=. 'ctx.lineTo' ,"1 (pfmtjs flipxy end >. 0) ,"1 ';ctx.stroke();ctx.fill();ctx.closePath();'
lin=. ,LF,.bgn,.end

res,lin
)


plotjijx_z_=: 3 : 0
canvasnum_jhs_=: >:canvasnum_jhs_
canvasname=. 'canvas',":canvasnum_jhs_
d=. fread y
c=. (('<canvas 'E.d)i.1)}.d
c=. (9+('</canvas>'E.c)i.1){.c
c=. c rplc 'canvas1';canvasname
d=. (('function graph()'E.d)i.1)}.d
d=. (('</script>'E.d)i.1){.d
d=. d,'graph();'
d=. d rplc'canvas1';canvasname
jhtml '<span id="coords', canvasname, '"></span><br>',c
jjsx d
)


canvasdot_hover_jzplot_ =: 3 : 0
pbuf 'hover.push(''' ,"1 (([: , ' '&,@":&>) "1 data_base_), "1 ''')'
)

Note 'test'

strToTable =: (< @ (".^:(#@".)) ) ;._2@,&' ';._2

data=: strToTable 'a 5 10',LF,'b 7 12',LF,'c 10 50',LF,'z 100 15',LF,'d 8 15',LF

'x y' =. > |: }."1 data

pd 'reset'
pd 'type dot'
pd 'pensize 10'
pd x;y
pd 'output canvas 800 400'
pd 'show'

)
