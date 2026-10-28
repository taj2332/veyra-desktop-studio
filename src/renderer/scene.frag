precision highp float;
uniform vec2 u_resolution,u_imageSize;
uniform float u_time,u_seed,u_mood,u_hasImage;
uniform int u_scene,u_effect;
uniform vec3 u_accent;
uniform sampler2D u_image;
// Original shader artwork. Smooth procedural fields remain resolution independent.
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7))+u_seed)*43758.5453);}
float noise(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.-2.*f);return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);}
float fbm(vec2 p){float a=.5,v=0.;mat2 m=mat2(.8,-.6,.6,.8);for(int i=0;i<4;i++){v+=a*noise(p);p=m*p*2.03+17.1;a*=.5;}return v;}
vec3 palette(float t){return .5+.5*cos(6.28318*(vec3(.04,.16,.31)+t*.28+u_accent*.28));}
float stars(vec2 p){vec2 g=p*vec2(220.,125.);vec2 id=floor(g);vec2 f=fract(g)-.5;float r=hash(id);return smoothstep(.055,0.,length(f))*step(.985,r)*(.45+.55*sin(u_time*.4+r*33.));}
vec3 aurora(vec2 uv,vec2 p,float t){vec3 c=mix(vec3(.012,.025,.075),vec3(.025,.12,.15),uv.y);for(int i=0;i<5;i++){float k=float(i),x=p.x*1.5+k*.7;float ribbon=.32+sin(x*.8+t*.14+k)*.14+sin(x*1.9-t*.11+k)*.04;float glow=exp(-abs(p.y-ribbon)*mix(7.,17.,k/5.));float rays=.45+.55*pow(noise(vec2(x*8.,t*.1+k)),2.);c+=mix(u_accent,vec3(.12,.9,.68),k/5.)*glow*rays*.15;}c+=stars(uv)*.7;float ridge=.13+fbm(vec2(p.x*2.,1.))*.19;c=mix(c,vec3(.008,.02,.035),smoothstep(ridge+.008,ridge-.008,uv.y));return c;}
vec3 liquid(vec2 p,float t){float flow=sin(p.x*1.6+sin(p.y*2.+t*.2)*.7-t*.16);float folds=sin(p.y*4.8+flow*2.3+t*.25);float v=.5+.5*folds;vec3 c=mix(vec3(.018,.025,.055),u_accent*.6,pow(v,2.));c+=pow(v,12.)*vec3(.7,.8,1.)*.25;float bands=sin((p.y+flow*.17)*60.+t*.5);c+=pow(.5+.5*bands,22.)*.035;return c;}
vec3 dunes(vec2 uv,vec2 p,float t){vec3 c=mix(vec3(.28,.19,.28),vec3(.9,.64,.44),uv.y);vec2 sun=vec2(.72,.74);c+=vec3(.5,.28,.12)*exp(-length(uv-sun)*5.);c=mix(c,vec3(1.,.87,.65),smoothstep(.078,.075,length(uv-sun)));for(int i=0;i<5;i++){float k=float(i);float h=.45-k*.075+sin(p.x*(1.1+k*.12)+k*1.7+t*.035)*(.06+k*.01);vec3 sand=mix(vec3(.78,.48,.35),vec3(.13,.10,.19),k/5.);sand=mix(sand,u_accent*.45,.16);float grain=noise(p*180.)*.013;c=mix(c,sand+grain,smoothstep(h+.002,h-.002,uv.y));}return c;}
vec3 space(vec2 uv,vec2 p,float t){float neb=pow(fbm(p*2.+vec2(t*.025,0)),3.);vec3 c=vec3(.006,.012,.035)+u_accent*neb*.5;c+=stars(uv)*.8;if(u_scene==5){vec2 q=p-vec2(.18,.06);float r=length(q);float disk=smoothstep(.28,.275,r);vec3 normal=vec3(q/.28,sqrt(max(0.,1.-r*r/.0784)));float light=max(0.,dot(normal,normalize(vec3(-.7,.5,1.))));vec3 sphere=mix(u_accent*.08,u_accent*.8,light);sphere+=sin(q.y*55.+sin(q.x*9.+t*.08))*light*.025;c=mix(c,sphere,disk);float ring=abs(length(vec2(q.x,q.y*3.7))-.43);c+=u_accent*exp(-ring*150.)*.5;}return c;}
vec3 ocean(vec2 uv,vec2 p,float t){vec3 c=mix(vec3(.008,.045,.095),u_accent*.35,uv.y);float y=p.y*3.+fbm(p*2.+t*.04)*.8;for(int i=0;i<5;i++){float k=float(i);float wave=sin(p.x*(2.+k*.3)+t*(.2+k*.03)+sin(p.x*3.-t*.2)*.3);float line=exp(-abs(y+wave*.17-k*.25+.5)*35.);c+=mix(u_accent,vec3(.72,.9,1.),k/5.)*line*.16;}c+=vec3(.06,.13,.18)*pow(max(0.,1.-abs(p.x-.45)),10.)*.3;return c;}
vec3 mountains(vec2 uv,vec2 p,float t){vec3 c=mix(vec3(.08,.12,.23),u_accent*.42,uv.y);for(int i=0;i<5;i++){float k=float(i),h=.56-k*.09+abs(sin(p.x*(1.5+k*.2)+k*8.))*.17+noise(vec2(p.x*8.,k))*.055;vec3 mountain=mix(u_accent*.45,vec3(.012,.035,.06),k/4.);float snow=smoothstep(.16,.22,h-uv.y);c=mix(c,mix(vec3(.65,.78,.84),mountain,snow),smoothstep(h+.003,h-.003,uv.y));}c+=u_accent*.025*sin(p.x*2.+t*.15);return c;}
vec3 forest(vec2 uv,vec2 p,float t){vec3 c=mix(vec3(.015,.045,.06),u_accent*.18,uv.y);for(int i=0;i<3;i++){float k=float(i),scale=10.+k*4.;float x=p.x*scale,id=floor(x),fx=fract(x)-.5;float h=.22+hash(vec2(id,k))*.35-k*.07;float tree=step(uv.y,h)*step(abs(fx),max(0.,h-uv.y)*1.6);c=mix(c,mix(u_accent*.1,vec3(.005,.018,.025),k/2.),tree);}c+=u_accent*exp(-length(p-vec2(.35,.45))*4.)*.1;return c;}
vec3 bloom(vec2 p,float t){vec3 c=vec3(.035,.018,.06);for(int i=0;i<7;i++){float k=float(i);vec2 q=p-vec2(sin(k*2.4+t*.11),cos(k*1.7-t*.09))*.45;float d=length(q);c+=mix(u_accent,vec3(.9,.4,.6),k/7.)*exp(-d*d*(7.+k))*.18;}return c;}
void main(){vec2 uv=gl_FragCoord.xy/u_resolution;vec2 p=(gl_FragCoord.xy-.5*u_resolution)/u_resolution.y;float t=u_time;vec3 c;
 if(u_scene==0||u_scene==1||u_scene==16){vec2 scale=vec2(1.);float ar=u_resolution.x/u_resolution.y,ia=u_imageSize.x/u_imageSize.y;if(ar>ia)scale.y=ia/ar;else scale.x=ar/ia;float zoom=1.025+sin(t*.12)*.012;vec2 tex=(uv-.5)*scale/zoom+.5+vec2(sin(t*.09)*.005,cos(t*.07)*.003);c=texture2D(u_image,tex).rgb;if(u_hasImage<.5)c=vec3(.012,.025,.05);if(u_scene==0)c+=u_accent*pow(fbm(p*3.+vec2(t*.03,0)),4.)*.1;if(u_scene==1)c+=u_accent*exp(-length(p-vec2(.4,.1))*3.)*(.018+.012*sin(t*.5));}
 else if(u_scene==3)c=aurora(uv,p,t);
 else if(u_scene==4)c=dunes(uv,p,t);
 else if(u_scene==5||u_scene==10||u_scene==9)c=space(uv,p,t);
 else if(u_scene==7||u_scene==15)c=ocean(uv,p,t);
 else if(u_scene==8)c=mountains(uv,p,t);
 else if(u_scene==12)c=forest(uv,p,t);
 else if(u_scene==6||u_scene==14)c=bloom(p,t);
 else if(u_scene==11)c=mix(vec3(.008,.02,.04),u_accent*.2,fbm(p*2.+t*.02));
 else c=liquid(p,t)*(u_scene==13?.65:1.);
 float particleStrength=u_mood==1.?.22:1.;
 if(u_effect==2||u_scene==11){vec2 rain=vec2(p.x+p.y*.17,p.y)*vec2(95.,7.);vec2 id=floor(rain);vec2 f=fract(rain);float drop=step(.92,hash(vec2(id.x,1.)))*exp(-abs(f.x-.5)*70.)*pow(fract(f.y+t*(.7+hash(vec2(id.x,2.))*.6)),10.);c+=vec3(.5,.68,.85)*drop*.32*particleStrength;}
 if(u_effect==3||u_effect==4||u_scene==9||u_scene==14||u_scene==12){for(int i=0;i<22;i++){float k=float(i),r=hash(vec2(k,4.));vec2 pos=vec2(hash(vec2(k,1.))-.5,fract(hash(vec2(k,2.))+t*(u_effect==3?.025:.009))-.5);pos.x*=u_resolution.x/u_resolution.y;pos.x+=sin(t*.25+k)*.035;float d=length(p-pos),size=u_effect==3?.003:.0018;float light=exp(-d*d/(size*size));vec3 color=u_effect==3?vec3(.8,.9,1.):u_accent;c+=color*light*(.3+r*.7)*particleStrength;if(u_effect!=3)c+=color*exp(-d*d/.0003)*.018*particleStrength;}}
 if(u_effect==1)c+=u_accent*pow(fbm(p*2.+vec2(t*.04,0)),3.)*.1;
 float vignette=smoothstep(1.1,.15,length(p*vec2(.55,.8)));c*=.72+.28*vignette;
 if(u_mood==1.){float l=dot(c,vec3(.2126,.7152,.0722));c=mix(vec3(l),c,.55)*.7;}if(u_mood==2.)c=c*vec3(.6,.65,.82);
 c+=((hash(gl_FragCoord.xy)-.5)/255.);gl_FragColor=vec4(clamp(c,0.,1.),1.);
}

