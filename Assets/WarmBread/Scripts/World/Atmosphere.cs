using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace WarmBread
{
    public sealed class Atmosphere : MonoBehaviour
    {
        private GameSession session;
        private Light sun;
        private AudioSource ambience, radio, effects;
        private AudioClip bell;
        private int station;
        private float teaCooldown;
        public string RadioName => station == 0 ? "ТИХАЯ ВОЛНА" : station == 1 ? "ВЕЧЕРНИЙ ДВОР" : "ВЫКЛЮЧЕНО";
        public float Volume { get; private set; } = .55f;
        public void Initialize(GameSession game, Light daylight, Camera camera)
        {
            session=game;sun=daylight;
            camera.backgroundColor=new Color(.43f,.49f,.5f);camera.clearFlags=CameraClearFlags.SolidColor;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            ambience=gameObject.AddComponent<AudioSource>();ambience.loop=true;ambience.volume=.15f;
            ambience.clip=Noise();ambience.Play();
            radio=gameObject.AddComponent<AudioSource>();radio.loop=true;radio.volume=.07f;radio.clip=Melody(0);radio.Play();
            effects=gameObject.AddComponent<AudioSource>();effects.volume=.12f;bell=Bell();
            EventBus.Chime+=Chime;
            Rain();
        }
        private void OnDestroy(){EventBus.Chime-=Chime;AudioListener.pause=false;}
        public void SetVolume(float value){Volume=Mathf.Clamp01(value);AudioListener.volume=Volume;}
        public void NextStation()
        {
            station=(station+1)%3;radio.Stop();
            if(station<2){radio.clip=Melody(station);radio.Play();}
            EventBus.Say("Радио • "+RadioName);
        }
        public void Tea()
        {
            if(Time.time<teaCooldown){EventBus.Say("Чай ещё слишком горячий. Посмотрим в окно.");return;}
            teaCooldown=Time.time+90;EventBus.Say("Чай с сахаром. За стеклом пахнет дождём. Всё будет хорошо.");Chime(.6f);
        }
        private void Chime(float pitch){if(effects==null)return;effects.pitch=pitch;effects.PlayOneShot(bell);}
        private void Update()
        {
            if(session==null)return;
            float daylight=Mathf.Sin(Mathf.InverseLerp(5,22,session.Hour)*Mathf.PI);
            sun.intensity=Mathf.Lerp(.08f,.75f,daylight);sun.transform.rotation=Quaternion.Euler(10+daylight*24,-35,0);
            RenderSettings.fogColor=Color.Lerp(new Color(.14f,.19f,.23f),new Color(.43f,.49f,.5f),daylight);
            if(Camera.main!=null)Camera.main.backgroundColor=RenderSettings.fogColor;
        }
        private void Rain()
        {
            var g=new GameObject("Дождь за окном");g.transform.SetParent(transform);g.transform.position=new Vector3(0,9,9);
            var p=g.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=p.main;main.startLifetime=1.1f;main.startSpeed=9;main.startSize=.012f;main.maxParticles=1600;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.startColor=new Color(.6f,.72f,.75f,.32f);
            var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(36,12,.1f);
            g.transform.rotation=Quaternion.Euler(90,0,0);var emission=p.emission;emission.rateOverTime=750;
            var renderer=p.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=6;
            var mat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));mat.SetColor("_BaseColor",new Color(.5f,.65f,.7f,.3f));renderer.sharedMaterial=mat;p.Play();
        }
        // Все звуки синтезированы для проекта. Записей реальных радиостанций нет.
        private static AudioClip Noise()
        {
            const int rate=22050;var data=new float[rate*6];var random=new System.Random(421);float previous=0;
            for(int i=0;i<data.Length;i++){previous=Mathf.Lerp(previous,(float)random.NextDouble()*2-1,.17f);data[i]=previous*.36f;}
            var clip=AudioClip.Create("Дождь • синтез",data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        private static AudioClip Melody(int variation)
        {
            const int rate=22050;const float beat=.6f;int[] notes=variation==0?new[]{57,60,64,67,64,60,55,60,59,62,65,69,65,62,55,59}:new[]{52,55,59,62,59,55,50,55,48,52,55,60,55,52,47,50};
            var data=new float[(int)(rate*beat*notes.Length)];
            for(int i=0;i<data.Length;i++)
            {
                float t=i/(float)rate;int index=(int)(t/beat)%notes.Length;float local=t%beat;
                float hz=440*Mathf.Pow(2,(notes[index]-69)/12f);
                float env=Mathf.Min(local*35,1)*Mathf.Exp(-local*5);
                data[i]=(Mathf.Sin(2*Mathf.PI*hz*t)+.18f*Mathf.Sin(2*Mathf.PI*hz*2*t))*env*.16f;
            }
            var clip=AudioClip.Create("Оригинальная радиопетля "+variation,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        private static AudioClip Bell()
        {
            const int rate=22050;var data=new float[rate/4];
            for(int i=0;i<data.Length;i++){float t=i/(float)rate;data[i]=Mathf.Sin(t*2*Mathf.PI*1100)*Mathf.Exp(-t*24)*.4f;}
            var clip=AudioClip.Create("Монета",data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
    }
}
