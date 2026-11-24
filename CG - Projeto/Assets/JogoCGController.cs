using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteAlways]
public class JogoCGController : MonoBehaviour
{
    // Trava automatica contra duplicacao caso o script tenha sido adicionado 2 vezes na Camera
    private static JogoCGController instanciaAtiva;

    [Header("Modelos 3D Personalizados (Opcional - Ja vem com Carro, Arvores e Drones Low Poly!)")]
    public GameObject modeloPlayerPrefab;
    public float escalaModeloPlayer = 1f;
    public GameObject modeloDronePrefab;
    public float escalaModeloDrone = 1f;

    [Header("Sons Personalizados (Opcional - Ja possui todos os sons e musica automaticos!)")]
    public AudioClip somCristalPersonalizado;
    public AudioClip somDanoPersonalizado;
    public AudioClip somVitoriaPersonalizado;
    public AudioClip somDerrotaPersonalizado;
    public AudioClip musicaFundoPersonalizada;

    [Header("Configuracoes do Jogador (Carro Low Poly Azul)")]
    public float velocidadeJogador = 9.5f;
    public float raioJogador = 0.75f;
    public float raioDrone = 0.75f;

    private GameObject jogador;
    private Vector3 posicaoInicial = new Vector3(0f, 0.45f, -13f);

    private int pontuacao = 0;
    private int totalCristais = 6;
    private int vidas = 3;
    private bool jogoVencido = false;
    private bool jogoPerdido = false;

    private string mensagemFeedback = "";
    private float tempoFeedback = 0f;

    private List<Transform> cristais = new List<Transform>();
    private List<PatrulheiroBezier> patrulheiros = new List<PatrulheiroBezier>();
    private List<Transform> barreirasMoveis = new List<Transform>();
    private List<Transform> pilares = new List<Transform>();
    private List<Transform> helicesDrones = new List<Transform>();
    private List<Transform> rodasCarro = new List<Transform>();

    private float raioPilar = 0.75f;

    // Pontos de controle das 2 Curvas de Bezier em formato de "S" (de uma parede a outra)
    private Vector3 c1_p0 = new Vector3(-10.7f, 0.9f, -2.5f);
    private Vector3 c1_p1 = new Vector3( -1.8f, 0.9f,  0.5f);
    private Vector3 c1_p2 = new Vector3( -4.5f, 0.9f, -12.5f);
    private Vector3 c1_p3 = new Vector3( 10.7f, 0.9f, -7.5f);

    private Vector3 c2_p0 = new Vector3(-10.7f, 0.9f,  3.5f);
    private Vector3 c2_p1 = new Vector3(  4.5f, 0.9f, -1.5f);
    private Vector3 c2_p2 = new Vector3(  1.8f, 0.9f, 11.5f);
    private Vector3 c2_p3 = new Vector3( 10.7f, 0.9f,  8.5f);

    private Camera cam;
    private Light luzSol;
    private Light luzJogador;
    private bool mostrarCurvasBezier = true;
    private List<LineRenderer> linhasBezier = new List<LineRenderer>();

    // Fontes de Audio separadas para Efeitos, Motor do Carro, Drones e Musica de Fundo
    private AudioSource audioEfeitos;
    private AudioSource audioMotorCarro;
    private AudioSource audioZumbidoDrone;
    private AudioSource audioMusicaFundo;

    private AudioClip somPontoGerado;
    private AudioClip somDanoGerado;
    private AudioClip somVitoriaGerado;
    private AudioClip somDerrotaGerado;

    private Material materialBaseCena;

    // Texturas geradas por codigo para deixar a interface (HUD) moderna e estilizada
    private Texture2D texFundoPainel;
    private Texture2D texBordaCiano;
    private Texture2D texFundoTecla;
    private Texture2D texBarraFundo;
    private Texture2D texBarraProgresso;

#if UNITY_EDITOR
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += GarantirShader3DNoExecutavel;
    }

    void GarantirShader3DNoExecutavel()
    {
        if (this == null || Application.isPlaying) return;
        if (GameObject.Find("_Shader3DKeeper") == null)
        {
            GameObject keeper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keeper.name = "_Shader3DKeeper";
            keeper.transform.position = new Vector3(0f, -500f, 0f);
            keeper.transform.localScale = Vector3.one * 0.01f;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
    }
#endif

    private class PatrulheiroBezier
    {
        public Transform objeto;
        public Vector3 p0, p1, p2, p3;
        public float velocidadeT;
        public float offsetTempo;
    }

    void Awake()
    {
        if (!Application.isPlaying) return;

        // Se ja existir outro JogoCGController ativo na cena, desativa e destroi a copia duplicada!
        if (instanciaAtiva != null && instanciaAtiva != this)
        {
            enabled = false;
            Destroy(this);
            return;
        }
        instanciaAtiva = this;
    }

    void OnDestroy()
    {
        if (instanciaAtiva == this)
        {
            instanciaAtiva = null;
        }
    }

    // Remove copias antigas antes de recriar para nunca duplicar objetos ao dar Play ou Stop
    void LimparObjetosGeradosNaCena()
    {
        string[] prefixos = new string[]
        {
            "Piso_Arena", "Parede", "Arvore_LowPoly", "Barreira_Movel_",
            "Jogador_Carro_LowPoly_Azul", "Jogador_Custom3D",
            "Drone_Bezier_", "Trilho_Curva_Bezier", "Cristal_", "Luz_Direcional_Sol"
        };

        GameObject[] todos = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in todos)
        {
            if (obj == null || obj == gameObject) continue;
            // So apaga objetos raiz que pertencam a lista de objetos do cenário
            if (obj.transform.parent != null) continue;

            foreach (string pref in prefixos)
            {
                if (obj.name.StartsWith(pref))
                {
                    if (Application.isPlaying)
                        Destroy(obj);
                    else
                        DestroyImmediate(obj);
                    break;
                }
            }
        }

        cristais.Clear();
        patrulheiros.Clear();
        barreirasMoveis.Clear();
        pilares.Clear();
        helicesDrones.Clear();
        rodasCarro.Clear();
        linhasBezier.Clear();
    }

    // Garante que mesmo dando STOP na Unity, todo o cenario continue visivel na Scene e na Hierarchy!
    void GarantirCenaVisivelNoModoStop()
    {
        if (Application.isPlaying) return;

        bool prefabsJaSalvos = true;
#if UNITY_EDITOR
        prefabsJaSalvos = UnityEditor.AssetDatabase.IsValidFolder("Assets/Materiais")
                       && UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Drone_Bezier_3D.prefab") != null;
#endif
        if (prefabsJaSalvos && (GameObject.Find("Jogador_Carro_LowPoly_Azul") != null || GameObject.Find("Jogador_Custom3D") != null))
        {
            return;
        }

        LimparObjetosGeradosNaCena();
        ConfigurarCamera();
        ConfigurarIluminacao();
        ConstruirArena();
        CriarJogador();
        CriarPatrulheirosBezier();
        CriarCristais();

        // Posiciona os drones e a camera na posicao inicial para a cena ficar linda mesmo em STOP
        foreach (PatrulheiroBezier pb in patrulheiros)
        {
            if (pb.objeto != null)
            {
                Vector3 posInicialDrone = CalcularPontoBezierCubico(0.15f, pb.p0, pb.p1, pb.p2, pb.p3);
                pb.objeto.position = CorrigirColisaoComCilindrosEParedes(posInicialDrone, raioDrone);
            }
        }

        if (cam != null && jogador != null)
        {
            cam.transform.position = new Vector3(jogador.transform.position.x * 0.4f, 13.5f, jogador.transform.position.z - 10f);
            cam.transform.LookAt(jogador.transform.position + new Vector3(0f, 0f, 2f));
        }

#if UNITY_EDITOR
        // Salva automaticamente os Prefabs 3D coloridos na pasta Assets para aparecerem com cor na parte de baixo!
        if (jogador != null)
        {
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(jogador, "Assets/Jogador_Carro_LowPoly_Azul.prefab");
        }
        if (patrulheiros.Count > 0 && patrulheiros[0].objeto != null)
        {
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(patrulheiros[0].objeto.gameObject, "Assets/Drone_Bezier_3D.prefab");
        }
        if (pilares.Count > 0 && pilares[0] != null)
        {
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(pilares[0].gameObject, "Assets/Arvore_LowPoly.prefab");
        }
        if (cristais.Count > 0 && cristais[0] != null)
        {
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(cristais[0].gameObject, "Assets/Cristal_Energia.prefab");
        }
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    void Start()
    {
        if (!Application.isPlaying)
        {
            GarantirCenaVisivelNoModoStop();
            return;
        }

        if (instanciaAtiva != this) return;

        LimparObjetosGeradosNaCena();
        ConfigurarCamera();
        ConfigurarAudio();
        ConfigurarIluminacao();
        ConstruirArena();
        CriarJogador();
        CriarPatrulheirosBezier();
        CriarCristais();
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            GarantirCenaVisivelNoModoStop();
            return;
        }

        if (instanciaAtiva != this) return;

        ProcessarTeclasDemonstracaoCG();

        if (tempoFeedback > 0f)
        {
            tempoFeedback -= Time.deltaTime;
        }

        if (jogoVencido || jogoPerdido)
        {
            if (audioMotorCarro != null) audioMotorCarro.volume = 0f;
            if (audioZumbidoDrone != null) audioZumbidoDrone.volume = 0f;

            if (TeclaPressionadaAgora(Key.R))
            {
                ReiniciarJogo();
            }
            return;
        }

        MoverJogadorComColisaoArvores();
        AtualizarCristais();
        AtualizarPatrulheirosBezier();
        AtualizarBarreiras();
        AtualizarCamera();
    }

    void ConfigurarCamera()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
        }

        cam.orthographic = false;
        cam.fieldOfView = 60f;
        cam.orthographicSize = 11f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 100f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.10f, 0.18f);
    }

    // Configura todos os sons do jogo: Efeitos (Cristal, Dano, Vitoria, Derrota), Motor, Drones e Musica!
    void ConfigurarAudio()
    {
#if UNITY_EDITOR
        // Desmuta automaticamente o botao 'Mute Audio' da aba Game na Unity caso esteja ativado!
        UnityEditor.EditorUtility.audioMasterMute = false;
#endif
        // Garante que existe um AudioListener ativo na cena e com volume no maximo
        AudioListener listener = FindAnyObjectByType<AudioListener>();
        if (listener == null)
        {
            gameObject.AddComponent<AudioListener>();
        }
        AudioListener.pause = false;
        AudioListener.volume = 1f;

        // 1. Canal de Efeitos (Cristal, Dano, Vitoria e Derrota)
        audioEfeitos = gameObject.AddComponent<AudioSource>();
        audioEfeitos.playOnAwake = false;
        audioEfeitos.spatialBlend = 0f;
        audioEfeitos.volume = 0.45f;

        somPontoGerado = GerarSomCristal();
        somDanoGerado = GerarSomDano();
        somVitoriaGerado = GerarSomVitoria();
        somDerrotaGerado = GerarSomDerrota();

        // 2. Canal do Motor do Carro (toca baixinho enquanto o carro anda)
        audioMotorCarro = gameObject.AddComponent<AudioSource>();
        audioMotorCarro.clip = GerarSomMotorCarroLoop();
        audioMotorCarro.loop = true;
        audioMotorCarro.spatialBlend = 0f;
        audioMotorCarro.volume = 0f;
        audioMotorCarro.Play();

        // 3. Canal dos Drones (som bem baixinho continuo das helices dos drones)
        audioZumbidoDrone = gameObject.AddComponent<AudioSource>();
        audioZumbidoDrone.clip = GerarSomDroneLoop();
        audioZumbidoDrone.loop = true;
        audioZumbidoDrone.spatialBlend = 0f;
        audioZumbidoDrone.volume = 0.025f;
        audioZumbidoDrone.Play();

        // 4. Canal da Musica de Fundo (100% original gerada por codigo = Zero Direitos Autorais!)
        audioMusicaFundo = gameObject.AddComponent<AudioSource>();
        audioMusicaFundo.clip = (musicaFundoPersonalizada != null) ? musicaFundoPersonalizada : GerarMusicaFundoSemCopyright();
        audioMusicaFundo.loop = true;
        audioMusicaFundo.spatialBlend = 0f;
        audioMusicaFundo.volume = 0.14f;
        audioMusicaFundo.Play();
    }

    AudioClip GerarSomCristal()
    {
        int sampleRate = 44100;
        int totalSamples = sampleRate / 3;
        float[] dados = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = (t < 0.12f) ? 880f : 1318.5f;
            float envelope = Mathf.Exp(-t * 6f);
            dados[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.85f;
        }

        AudioClip clip = AudioClip.Create("SomCristal", totalSamples, 1, sampleRate, false);
        clip.SetData(dados, 0);
        return clip;
    }

    AudioClip GerarSomDano()
    {
        int sampleRate = 44100;
        int totalSamples = sampleRate / 2;
        float[] dados = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(300f, 95f, t * 2f);
            float envelope = Mathf.Exp(-t * 4f);
            float onda = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t);
            dados[i] = Mathf.Clamp(onda * envelope * 0.85f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("SomDano", totalSamples, 1, sampleRate, false);
        clip.SetData(dados, 0);
        return clip;
    }

    AudioClip GerarSomVitoria()
    {
        int sampleRate = 44100;
        int totalSamples = (int)(sampleRate * 1.2f);
        float[] dados = new float[totalSamples];
        float[] notas = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            int indiceNota = Mathf.Min(3, (int)(t / 0.22f));
            float tempoNaNota = t - (indiceNota * 0.22f);
            float freq = notas[indiceNota];
            float decaimento = (indiceNota == 3) ? 2.2f : 6f;
            float envelope = Mathf.Exp(-tempoNaNota * decaimento);
            float onda = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * (freq * 2f) * t);
            dados[i] = Mathf.Clamp(onda * envelope * 0.85f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("SomVitoria", totalSamples, 1, sampleRate, false);
        clip.SetData(dados, 0);
        return clip;
    }

    // Melodia descendente de 4 notas graves (Game Over / Derrota ao perder as 3 vidas!)
    AudioClip GerarSomDerrota()
    {
        int sampleRate = 44100;
        int totalSamples = (int)(sampleRate * 1.35f);
        float[] dados = new float[totalSamples];
        float[] notas = new float[] { 392.00f, 369.99f, 349.23f, 293.66f };

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            int indiceNota = Mathf.Min(3, (int)(t / 0.26f));
            float tempoNaNota = t - (indiceNota * 0.26f);
            float freq = notas[indiceNota];

            if (indiceNota == 3)
            {
                freq = Mathf.Lerp(293.66f, 220.0f, tempoNaNota * 1.4f);
            }

            float decaimento = (indiceNota == 3) ? 2.0f : 5.5f;
            float envelope = Mathf.Exp(-tempoNaNota * decaimento);
            float onda = Mathf.Sin(2f * Mathf.PI * freq * t)
                       + 0.45f * Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t)
                       + 0.25f * Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t);
            dados[i] = Mathf.Clamp(onda * envelope * 0.85f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("SomDerrota", totalSamples, 1, sampleRate, false);
        clip.SetData(dados, 0);
        return clip;
    }

    AudioClip GerarSomMotorCarroLoop()
    {
        int sampleRate = 44100;
        int totalSamples = sampleRate;
        float[] dados = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            // Harmonicos medios (110Hz a 440Hz) para ser bem audivel em caixas de som de notebook e monitor
            float pulsacao = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 18f * t);
            float onda = 0.45f * Mathf.Sin(2f * Mathf.PI * 110f * t)
                       + 0.35f * Mathf.Sin(2f * Mathf.PI * 220f * t)
                       + 0.20f * Mathf.Sin(2f * Mathf.PI * 330f * t);
            dados[i] = Mathf.Clamp(onda * pulsacao * 0.75f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("SomMotorCarro", totalSamples, 1, sampleRate, false);
        clip.SetData(dados, 0);
        return clip;
    }

    AudioClip GerarSomDroneLoop()
    {
        int sampleRate = 44100;
        int totalSamples = sampleRate;
        float[] dados = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            float modulacaoHelice = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 24f * t);
            float onda = 0.5f * Mathf.Sin(2f * Mathf.PI * 240f * t)
                       + 0.35f * Mathf.Sin(2f * Mathf.PI * 480f * t)
                       + 0.15f * Mathf.Sin(2f * Mathf.PI * 720f * t);
            dados[i] = Mathf.Clamp(onda * modulacaoHelice * 0.65f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("SomDroneLoop", totalSamples, 1, sampleRate, false);
        clip.SetData(dados, 0);
        return clip;
    }

    AudioClip GerarMusicaFundoSemCopyright()
    {
        int sampleRate = 44100;
        float duracaoNota = 0.4f;
        float[] melodia = new float[]
        {
            261.63f, 329.63f, 392.00f, 523.25f,
            392.00f, 329.63f, 293.66f, 392.00f,
            220.00f, 261.63f, 329.63f, 440.00f,
            329.63f, 261.63f, 246.94f, 392.00f
        };
        float[] baixo = new float[]
        {
            130.81f, 130.81f, 130.81f, 130.81f,
            196.00f, 196.00f, 196.00f, 196.00f,
            220.00f, 220.00f, 220.00f, 220.00f,
            174.61f, 174.61f, 196.00f, 196.00f
        };

        int totalSamples = (int)(sampleRate * melodia.Length * duracaoNota);
        float[] dados = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            int idx = Mathf.Min(melodia.Length - 1, (int)(t / duracaoNota));
            float tNota = t - (idx * duracaoNota);

            float freqMelodia = melodia[idx];
            float freqBaixo = baixo[idx];

            float envMelodia = Mathf.Exp(-tNota * 3.5f);
            float envBaixo = Mathf.Exp(-tNota * 1.8f);

            float somMelodia = Mathf.Sin(2f * Mathf.PI * freqMelodia * t) * envMelodia * 0.50f;
            float somBaixo = Mathf.Sin(2f * Mathf.PI * freqBaixo * t) * envBaixo * 0.35f;

            dados[i] = Mathf.Clamp(somMelodia + somBaixo, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("MusicaSemCopyright", totalSamples, 1, sampleRate, false);
        clip.SetData(dados, 0);
        return clip;
    }

    void TocarSomCristal()
    {
        AudioClip clip = (somCristalPersonalizado != null) ? somCristalPersonalizado : somPontoGerado;
        if (audioEfeitos != null && clip != null) audioEfeitos.PlayOneShot(clip, 1f);
    }

    void TocarSomDano()
    {
        AudioClip clip = (somDanoPersonalizado != null) ? somDanoPersonalizado : somDanoGerado;
        if (audioEfeitos != null && clip != null) audioEfeitos.PlayOneShot(clip, 1f);
    }

    void TocarSomVitoria()
    {
        AudioClip clip = (somVitoriaPersonalizado != null) ? somVitoriaPersonalizado : somVitoriaGerado;
        if (audioEfeitos != null && clip != null) audioEfeitos.PlayOneShot(clip, 1f);
    }

    void TocarSomDerrota()
    {
        AudioClip clip = (somDerrotaPersonalizado != null) ? somDerrotaPersonalizado : somDerrotaGerado;
        if (audioEfeitos != null && clip != null) audioEfeitos.PlayOneShot(clip, 1f);
    }

    void ConfigurarIluminacao()
    {
        Light[] luzes = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in luzes)
        {
            if (l.type == LightType.Directional) luzSol = l;
        }

        if (luzSol == null)
        {
            GameObject solObj = new GameObject("Luz_Direcional_Sol");
            luzSol = solObj.AddComponent<Light>();
            luzSol.type = LightType.Directional;
        }

        luzSol.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        luzSol.color = new Color(1f, 0.96f, 0.88f);
        luzSol.intensity = 1.1f;
        luzSol.shadows = LightShadows.Soft;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.25f, 0.35f);
    }

    GameObject ConstruirArvoreLowPoly(Vector3 posicaoChao)
    {
        GameObject raizArvore = new GameObject("Arvore_LowPoly");
        raizArvore.transform.position = new Vector3(posicaoChao.x, 0f, posicaoChao.z);

        GameObject tronco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tronco.name = "Tronco";
        tronco.transform.SetParent(raizArvore.transform);
        tronco.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        tronco.transform.localScale = new Vector3(0.75f, 0.8f, 0.75f);
        AplicarMaterial(tronco, new Color(0.42f, 0.25f, 0.12f), 0.1f, 0.25f);

        GameObject copa1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        copa1.name = "Copa_Inferior";
        copa1.transform.SetParent(raizArvore.transform);
        copa1.transform.localPosition = new Vector3(0f, 1.85f, 0f);
        copa1.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
        copa1.transform.localScale = new Vector3(1.75f, 0.85f, 1.75f);
        AplicarMaterial(copa1, new Color(0.14f, 0.52f, 0.20f), 0.15f, 0.35f);

        GameObject copa2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        copa2.name = "Copa_Media";
        copa2.transform.SetParent(raizArvore.transform);
        copa2.transform.localPosition = new Vector3(0f, 2.55f, 0f);
        copa2.transform.localRotation = Quaternion.Euler(0f, 70f, 0f);
        copa2.transform.localScale = new Vector3(1.30f, 0.75f, 1.30f);
        AplicarMaterial(copa2, new Color(0.20f, 0.65f, 0.25f), 0.15f, 0.40f);

        GameObject copa3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        copa3.name = "Copa_Topo";
        copa3.transform.SetParent(raizArvore.transform);
        copa3.transform.localPosition = new Vector3(0f, 3.15f, 0f);
        copa3.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        copa3.transform.localScale = new Vector3(0.82f, 0.65f, 0.82f);
        AplicarMaterial(copa3, new Color(0.28f, 0.78f, 0.32f), 0.15f, 0.45f);

        return raizArvore;
    }

    void ConstruirArena()
    {
        GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piso.name = "Piso_Arena";
        piso.transform.position = new Vector3(0f, -0.5f, 0f);
        piso.transform.localScale = new Vector3(24f, 1f, 32f);
        AplicarMaterial(piso, new Color(0.18f, 0.24f, 0.28f), 0.2f, 0.4f);

        CriarParede(new Vector3(0f, 0.75f, 16f), new Vector3(24f, 2.5f, 0.6f), new Color(0.25f, 0.35f, 0.55f));
        CriarParede(new Vector3(0f, 0.75f, -16f), new Vector3(24f, 2.5f, 0.6f), new Color(0.25f, 0.35f, 0.55f));
        CriarParede(new Vector3(-12f, 0.75f, 0f), new Vector3(0.6f, 2.5f, 32f), new Color(0.25f, 0.35f, 0.55f));
        CriarParede(new Vector3(12f, 0.75f, 0f), new Vector3(0.6f, 2.5f, 32f), new Color(0.25f, 0.35f, 0.55f));

        Vector3[] posArvores = new Vector3[]
        {
            new Vector3(-6.5f, 0f, -5f),
            new Vector3( 0.0f, 0f, -5f),
            new Vector3( 6.5f, 0f, -5f),

            new Vector3(-6.5f, 0f,  6f),
            new Vector3( 0.0f, 0f,  6f),
            new Vector3( 6.5f, 0f,  6f)
        };

        foreach (Vector3 pos in posArvores)
        {
            GameObject arvore = ConstruirArvoreLowPoly(pos);
            pilares.Add(arvore.transform);
        }

        for (int i = 0; i < 2; i++)
        {
            GameObject barreira = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barreira.name = "Barreira_Movel_" + i;
            float zPos = (i == 0) ? -9.5f : 0.5f;
            barreira.transform.position = new Vector3(0f, 0.6f, zPos);
            barreira.transform.localScale = new Vector3(4.2f, 1.2f, 0.6f);
            AplicarMaterial(barreira, new Color(0.85f, 0.35f, 0.15f), 0.4f, 0.6f);
            barreirasMoveis.Add(barreira.transform);
        }
    }

    void CriarParede(Vector3 posicao, Vector3 escala, Color cor)
    {
        GameObject parede = GameObject.CreatePrimitive(PrimitiveType.Cube);
        parede.name = "Parede";
        parede.transform.position = posicao;
        parede.transform.localScale = escala;
        AplicarMaterial(parede, cor, 0.1f, 0.3f);
    }

    GameObject ConstruirCarroLowPolyAzul()
    {
        rodasCarro.Clear();
        GameObject raizCarro = new GameObject("Jogador_Carro_LowPoly_Azul");
        raizCarro.transform.position = posicaoInicial;

        GameObject chassi = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chassi.name = "Chassi_Azul";
        chassi.transform.SetParent(raizCarro.transform);
        chassi.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        chassi.transform.localScale = new Vector3(0.95f, 0.36f, 1.75f);
        AplicarMaterial(chassi, new Color(0.06f, 0.52f, 0.98f), 0.55f, 0.88f);

        GameObject cabine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabine.name = "Cabine_Carro";
        cabine.transform.SetParent(raizCarro.transform);
        cabine.transform.localPosition = new Vector3(0f, 0.40f, -0.08f);
        cabine.transform.localScale = new Vector3(0.76f, 0.30f, 0.88f);
        AplicarMaterial(cabine, new Color(0.12f, 0.18f, 0.28f), 0.8f, 0.95f);

        GameObject teto = GameObject.CreatePrimitive(PrimitiveType.Cube);
        teto.name = "Teto_Azul";
        teto.transform.SetParent(raizCarro.transform);
        teto.transform.localPosition = new Vector3(0f, 0.56f, -0.08f);
        teto.transform.localScale = new Vector3(0.72f, 0.06f, 0.80f);
        AplicarMaterial(teto, new Color(0.08f, 0.60f, 1.0f), 0.55f, 0.88f);

        GameObject spoiler = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spoiler.name = "Aerofolio_Traseiro";
        spoiler.transform.SetParent(raizCarro.transform);
        spoiler.transform.localPosition = new Vector3(0f, 0.42f, -0.78f);
        spoiler.transform.localScale = new Vector3(0.92f, 0.08f, 0.22f);
        AplicarMaterial(spoiler, new Color(0.03f, 0.30f, 0.72f), 0.6f, 0.85f);

        for (int i = -1; i <= 1; i += 2)
        {
            GameObject farol = GameObject.CreatePrimitive(PrimitiveType.Cube);
            farol.name = "Farol";
            farol.transform.SetParent(raizCarro.transform);
            farol.transform.localPosition = new Vector3(i * 0.32f, 0.16f, 0.88f);
            farol.transform.localScale = new Vector3(0.20f, 0.12f, 0.05f);
            AplicarMaterial(farol, new Color(1f, 0.95f, 0.3f), 0.2f, 0.95f);
        }

        Vector3[] posRodas = new Vector3[]
        {
            new Vector3(-0.50f, 0.0f,  0.55f),
            new Vector3( 0.50f, 0.0f,  0.55f),
            new Vector3(-0.50f, 0.0f, -0.55f),
            new Vector3( 0.50f, 0.0f, -0.55f)
        };

        foreach (Vector3 pr in posRodas)
        {
            GameObject roda = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            roda.name = "Roda_Carro";
            roda.transform.SetParent(raizCarro.transform);
            roda.transform.localPosition = pr;
            roda.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            roda.transform.localScale = new Vector3(0.42f, 0.10f, 0.42f);
            AplicarMaterial(roda, new Color(0.12f, 0.12f, 0.14f), 0.3f, 0.4f);
            rodasCarro.Add(roda.transform);
        }

        return raizCarro;
    }

    void CriarJogador()
    {
        if (modeloPlayerPrefab != null)
        {
            jogador = Instantiate(modeloPlayerPrefab, posicaoInicial, Quaternion.identity);
            jogador.name = "Jogador_Custom3D";
            jogador.transform.localScale = Vector3.one * escalaModeloPlayer;
        }
        else
        {
            jogador = ConstruirCarroLowPolyAzul();
        }

        GameObject luzObj = new GameObject("Luz_Pontual_Jogador");
        luzObj.transform.SetParent(jogador.transform);
        luzObj.transform.localPosition = new Vector3(0f, 1.3f, 0f);
        luzJogador = luzObj.AddComponent<Light>();
        luzJogador.type = LightType.Point;
        luzJogador.color = new Color(0.2f, 0.85f, 1f);
        luzJogador.range = 8.5f;
        luzJogador.intensity = 1.8f;
    }

    void CriarCristais()
    {
        foreach (Transform c in cristais)
        {
            if (c != null) Destroy(c.gameObject);
        }
        cristais.Clear();

        Vector3 pontoCurva1A = CorrigirColisaoComCilindrosEParedes(CalcularPontoBezierCubico(0.25f, c1_p0, c1_p1, c1_p2, c1_p3), raioDrone);
        Vector3 pontoCurva1B = CorrigirColisaoComCilindrosEParedes(CalcularPontoBezierCubico(0.68f, c1_p0, c1_p1, c1_p2, c1_p3), raioDrone);
        Vector3 pontoCurva2A = CorrigirColisaoComCilindrosEParedes(CalcularPontoBezierCubico(0.32f, c2_p0, c2_p1, c2_p2, c2_p3), raioDrone);
        Vector3 pontoCurva2B = CorrigirColisaoComCilindrosEParedes(CalcularPontoBezierCubico(0.75f, c2_p0, c2_p1, c2_p2, c2_p3), raioDrone);

        Vector3[] posicoesCristais = new Vector3[]
        {
            new Vector3(9.5f, 0.9f, 0.5f),
            new Vector3(0f, 0.9f, 13.0f),
            new Vector3(pontoCurva1A.x, 0.9f, pontoCurva1A.z),
            new Vector3(pontoCurva1B.x, 0.9f, pontoCurva1B.z),
            new Vector3(pontoCurva2A.x, 0.9f, pontoCurva2A.z),
            new Vector3(pontoCurva2B.x, 0.9f, pontoCurva2B.z)
        };

        totalCristais = posicoesCristais.Length;

        for (int i = 0; i < posicoesCristais.Length; i++)
        {
            GameObject cristal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cristal.name = "Cristal_" + i;
            cristal.transform.position = posicoesCristais[i];
            cristal.transform.rotation = Quaternion.Euler(45f, 0f, 45f);
            cristal.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            AplicarMaterial(cristal, new Color(0.2f, 1f, 0.4f), 0.5f, 0.85f);
            cristais.Add(cristal.transform);
        }
    }

    Vector3 CorrigirColisaoComCilindrosEParedes(Vector3 posicaoDesejada, float raioObjeto)
    {
        float limiteX = 11.5f - raioObjeto;
        float limiteZ = 15.5f - raioObjeto;
        posicaoDesejada.x = Mathf.Clamp(posicaoDesejada.x, -limiteX, limiteX);
        posicaoDesejada.z = Mathf.Clamp(posicaoDesejada.z, -limiteZ, limiteZ);

        float distanciaMinima = raioPilar + raioObjeto;
        foreach (Transform arvore in pilares)
        {
            if (arvore == null) continue;
            Vector2 posObjXZ = new Vector2(posicaoDesejada.x, posicaoDesejada.z);
            Vector2 posArvoreXZ = new Vector2(arvore.position.x, arvore.position.z);
            Vector2 diff = posObjXZ - posArvoreXZ;
            float dist = diff.magnitude;

            if (dist < distanciaMinima)
            {
                if (dist < 0.001f) diff = new Vector2(0f, 1f);
                Vector2 posEmpurrada = posArvoreXZ + diff.normalized * distanciaMinima;
                posicaoDesejada.x = posEmpurrada.x;
                posicaoDesejada.z = posEmpurrada.y;
            }
        }

        return posicaoDesejada;
    }

    void MoverJogadorComColisaoArvores()
    {
        Vector2 entrada = LerMovimentoTeclado();
        Vector3 direcao = new Vector3(entrada.x, 0f, entrada.y).normalized;
        bool estaAndando = direcao.sqrMagnitude > 0.01f;

        if (audioMotorCarro != null)
        {
            float volumeAlvo = estaAndando ? 0.15f : 0f;
            audioMotorCarro.volume = Mathf.Lerp(audioMotorCarro.volume, volumeAlvo, 10f * Time.deltaTime);
            audioMotorCarro.pitch = estaAndando ? 1.08f : 0.9f;
        }

        if (estaAndando)
        {
            jogador.transform.forward = Vector3.Slerp(jogador.transform.forward, direcao, 14f * Time.deltaTime);
            foreach (Transform roda in rodasCarro)
            {
                if (roda != null) roda.Rotate(Vector3.up, 550f * Time.deltaTime, Space.Self);
            }
        }

        Vector3 novaPosicao = jogador.transform.position + direcao * velocidadeJogador * Time.deltaTime;
        jogador.transform.position = CorrigirColisaoComCilindrosEParedes(novaPosicao, raioJogador);
    }

    void AtualizarCristais()
    {
        for (int i = cristais.Count - 1; i >= 0; i--)
        {
            Transform c = cristais[i];
            if (c == null) continue;

            c.Rotate(new Vector3(45f, 90f, 30f) * Time.deltaTime, Space.World);

            float fatorEscala = 0.75f + 0.20f * Mathf.Sin(Time.time * 4f + i);
            c.localScale = new Vector3(fatorEscala, fatorEscala, fatorEscala);

            if (Vector3.Distance(jogador.transform.position, c.position) < 1.35f)
            {
                Destroy(c.gameObject);
                cristais.RemoveAt(i);
                pontuacao++;

                if (pontuacao >= totalCristais)
                {
                    jogoVencido = true;
                    TocarSomVitoria();
                    mensagemFeedback = "VITORIA! TODOS OS CRISTAIS COLETADOS!";
                    tempoFeedback = 5.0f;
                }
                else
                {
                    TocarSomCristal();
                    mensagemFeedback = "+1 CRISTAL COLETADO! (" + pontuacao + "/" + totalCristais + ")";
                    tempoFeedback = 1.8f;
                }
            }
        }
    }

    void AtualizarBarreiras()
    {
        for (int i = 0; i < barreirasMoveis.Count; i++)
        {
            Transform b = barreirasMoveis[i];
            float direcao = (i % 2 == 0) ? 1f : -1f;

            Vector3 pos = b.position;
            pos.x = Mathf.Sin(Time.time * 1.8f * direcao) * 6.5f;
            b.position = pos;

            Vector3 diff = jogador.transform.position - b.position;
            if (Mathf.Abs(diff.x) < 2.6f && Mathf.Abs(diff.z) < 0.9f)
            {
                PerderVida("Colisao com a Barreira Movel!");
            }
        }
    }

    void CriarPatrulheirosBezier()
    {
        CriarUmPatrulheiro(c1_p0, c1_p1, c1_p2, c1_p3, 0.28f, 0f);
        CriarUmPatrulheiro(c2_p0, c2_p1, c2_p2, c2_p3, 0.32f, 1.5f);
    }

    GameObject ConstruirModeloDrone3D()
    {
        if (modeloDronePrefab != null)
        {
            GameObject customDrone = Instantiate(modeloDronePrefab);
            customDrone.name = "Drone_Bezier_Custom3D";
            customDrone.transform.localScale = Vector3.one * escalaModeloDrone;
            return customDrone;
        }

        GameObject raizDrone = new GameObject("Drone_Bezier_3D");

        GameObject corpo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        corpo.transform.SetParent(raizDrone.transform);
        corpo.transform.localPosition = Vector3.zero;
        corpo.transform.localScale = new Vector3(1.0f, 0.45f, 1.0f);
        AplicarMaterial(corpo, new Color(0.95f, 0.15f, 0.2f), 0.7f, 0.9f);

        GameObject cupula = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cupula.transform.SetParent(raizDrone.transform);
        cupula.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        cupula.transform.localScale = new Vector3(0.55f, 0.35f, 0.55f);
        AplicarMaterial(cupula, new Color(0.15f, 0.15f, 0.2f), 0.8f, 0.95f);

        for (int i = 0; i < 2; i++)
        {
            GameObject braco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            braco.transform.SetParent(raizDrone.transform);
            braco.transform.localPosition = Vector3.zero;
            braco.transform.localRotation = Quaternion.Euler(0f, 45f + i * 90f, 0f);
            braco.transform.localScale = new Vector3(1.8f, 0.1f, 0.14f);
            AplicarMaterial(braco, new Color(0.25f, 0.25f, 0.3f), 0.6f, 0.5f);
        }

        Vector3[] cantos = new Vector3[]
        {
            new Vector3(0.6f, 0.12f, 0.6f),
            new Vector3(-0.6f, 0.12f, 0.6f),
            new Vector3(0.6f, 0.12f, -0.6f),
            new Vector3(-0.6f, 0.12f, -0.6f)
        };

        foreach (Vector3 canto in cantos)
        {
            GameObject helice = GameObject.CreatePrimitive(PrimitiveType.Cube);
            helice.name = "Helice_Drone";
            helice.transform.SetParent(raizDrone.transform);
            helice.transform.localPosition = canto;
            helice.transform.localScale = new Vector3(0.65f, 0.03f, 0.10f);
            AplicarMaterial(helice, new Color(1f, 0.85f, 0.1f), 0.3f, 0.8f);
            helicesDrones.Add(helice.transform);
        }

        return raizDrone;
    }

    void CriarUmPatrulheiro(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float velT, float offset)
    {
        GameObject drone = ConstruirModeloDrone3D();

        PatrulheiroBezier pb = new PatrulheiroBezier
        {
            objeto = drone.transform,
            p0 = p0,
            p1 = p1,
            p2 = p2,
            p3 = p3,
            velocidadeT = velT,
            offsetTempo = offset
        };
        patrulheiros.Add(pb);

        GameObject linhaObj = new GameObject("Trilho_Curva_Bezier");
        LineRenderer lr = linhaObj.AddComponent<LineRenderer>();
        int numSegmentos = 80;
        lr.positionCount = numSegmentos + 1;
        lr.startWidth = 0.14f;
        lr.endWidth = 0.14f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0.85f, 0.1f, 0.9f);
        lr.endColor = new Color(1f, 0.25f, 0.2f, 0.9f);

        for (int i = 0; i <= numSegmentos; i++)
        {
            float t = (float)i / numSegmentos;
            Vector3 pontoNaCurva = CalcularPontoBezierCubico(t, p0, p1, p2, p3);
            pontoNaCurva = CorrigirColisaoComCilindrosEParedes(pontoNaCurva, raioDrone);
            lr.SetPosition(i, new Vector3(pontoNaCurva.x, 0.08f, pontoNaCurva.z));
        }
        linhasBezier.Add(lr);
    }

    Vector3 CalcularPontoBezierCubico(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1f - t;
        float b0 = u * u * u;
        float b1 = 3f * u * u * t;
        float b2 = 3f * u * t * t;
        float b3 = t * t * t;

        return b0 * p0 + b1 * p1 + b2 * p2 + b3 * p3;
    }

    void AtualizarPatrulheirosBezier()
    {
        foreach (Transform h in helicesDrones)
        {
            if (h != null) h.Rotate(Vector3.up, 900f * Time.deltaTime, Space.Self);
        }

        float menorDistanciaDrone = 999f;

        foreach (PatrulheiroBezier pb in patrulheiros)
        {
            float t = Mathf.PingPong((Time.time + pb.offsetTempo) * pb.velocidadeT, 1f);
            Vector3 posCurva = CalcularPontoBezierCubico(t, pb.p0, pb.p1, pb.p2, pb.p3);
            Vector3 posSegura = CorrigirColisaoComCilindrosEParedes(posCurva, raioDrone);

            Vector3 deslocamento = posSegura - pb.objeto.position;
            deslocamento.y = 0f;
            if (deslocamento.sqrMagnitude > 0.0001f)
            {
                pb.objeto.forward = Vector3.Slerp(pb.objeto.forward, deslocamento.normalized, 10f * Time.deltaTime);
            }

            pb.objeto.position = posSegura;

            float dist = Vector3.Distance(jogador.transform.position, pb.objeto.position);
            if (dist < menorDistanciaDrone) menorDistanciaDrone = dist;

            if (dist < 1.3f)
            {
                PerderVida("Atingido pelo Drone de Bezier!");
            }
        }

        if (audioZumbidoDrone != null)
        {
            float fatorProximidade = Mathf.Clamp01(1f - (menorDistanciaDrone / 18f));
            audioZumbidoDrone.volume = 0.015f + 0.035f * fatorProximidade;
        }
    }

    void PerderVida(string motivo)
    {
        vidas--;
        jogador.transform.position = posicaoInicial;
        jogador.transform.rotation = Quaternion.identity;

        mensagemFeedback = "-1 VIDA! " + motivo;
        tempoFeedback = 2.0f;

        if (vidas <= 0)
        {
            jogoPerdido = true;
            // Toca a melodia de Derrota (Game Over) quando perde a 3a e ultima vida!
            TocarSomDerrota();
        }
        else
        {
            TocarSomDano();
        }
    }

    void ReiniciarJogo()
    {
        vidas = 3;
        pontuacao = 0;
        jogoVencido = false;
        jogoPerdido = false;
        mensagemFeedback = "";
        jogador.transform.position = posicaoInicial;
        jogador.transform.rotation = Quaternion.identity;
        CriarCristais();
    }

    void AtualizarCamera()
    {
        Vector3 posAlvo = new Vector3(jogador.transform.position.x * 0.4f, 13.5f, jogador.transform.position.z - 10f);
        cam.transform.position = Vector3.Lerp(cam.transform.position, posAlvo, 5f * Time.deltaTime);
        cam.transform.LookAt(jogador.transform.position + new Vector3(0f, 0f, 2f));
    }

    void ProcessarTeclasDemonstracaoCG()
    {
        if (TeclaPressionadaAgora(Key.P))
        {
            cam.orthographic = !cam.orthographic;
        }

        if (TeclaSegurada(Key.L))
        {
            luzSol.transform.Rotate(Vector3.up, 60f * Time.deltaTime, Space.World);
        }

        if (TeclaPressionadaAgora(Key.B))
        {
            mostrarCurvasBezier = !mostrarCurvasBezier;
            foreach (LineRenderer lr in linhasBezier)
            {
                lr.enabled = mostrarCurvasBezier;
            }
        }

        if (TeclaPressionadaAgora(Key.C))
        {
            cam.farClipPlane = (cam.farClipPlane > 40f) ? 27f : 100f;
        }

        // Tecla ESC: Fecha o jogo quando estiver rodando no executavel (.exe)
        if (TeclaPressionadaAgora(Key.Escape))
        {
            Application.Quit();
        }
    }

    Vector2 LerMovimentoTeclado()
    {
        float x = 0f, y = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
        }
        return new Vector2(x, y);
    }

    bool TeclaPressionadaAgora(Key tecla)
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current[tecla].wasPressedThisFrame;
    }

    bool TeclaSegurada(Key tecla)
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current[tecla].isPressed;
    }

    void AplicarMaterial(GameObject obj, Color cor, float metalico, float brilho)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend == null) return;

        if (materialBaseCena == null)
        {
            if (rend.sharedMaterial != null && rend.sharedMaterial.shader != null)
            {
                materialBaseCena = rend.sharedMaterial;
            }
            else
            {
                GameObject keeper = GameObject.Find("_Shader3DKeeper");
                if (keeper != null && keeper.GetComponent<Renderer>() != null)
                {
                    materialBaseCena = keeper.GetComponent<Renderer>().sharedMaterial;
                }
            }
        }

        Material mat;
        if (materialBaseCena != null)
        {
            mat = new Material(materialBaseCena);
        }
        else
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Legacy Shaders/Diffuse");
            if (s == null) s = Shader.Find("Sprites/Default");
            mat = new Material(s);
        }

        mat.color = cor;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", cor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", cor);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metalico);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", brilho);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", brilho);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Materiais"))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Materiais");
            }
            string codigoCor = ColorUtility.ToHtmlStringRGBA(cor);
            string caminhoMat = "Assets/Materiais/Mat_" + codigoCor + ".mat";
            Material matSalvo = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(caminhoMat);
            if (matSalvo == null)
            {
                UnityEditor.AssetDatabase.CreateAsset(mat, caminhoMat);
                matSalvo = mat;
            }
            rend.sharedMaterial = matSalvo;
            return;
        }
#endif

        rend.material = mat;
    }

    Texture2D CriarTexturaCor(Color cor)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, cor);
        tex.Apply();
        return tex;
    }

    void InicializarTexturasUI()
    {
        if (texFundoPainel == null) texFundoPainel = CriarTexturaCor(new Color(0.05f, 0.08f, 0.15f, 0.90f));
        if (texBordaCiano == null) texBordaCiano = CriarTexturaCor(new Color(0.10f, 0.75f, 1.00f, 0.95f));
        if (texFundoTecla == null) texFundoTecla = CriarTexturaCor(new Color(0.13f, 0.22f, 0.36f, 0.95f));
        if (texBarraFundo == null) texBarraFundo = CriarTexturaCor(new Color(0.12f, 0.15f, 0.22f, 0.95f));
        if (texBarraProgresso == null) texBarraProgresso = CriarTexturaCor(new Color(0.18f, 0.92f, 0.48f, 0.95f));
    }

    void DesenharLinhaComando(float x, float y, string tecla, string descricao, GUIStyle estiloTecla, GUIStyle estiloDesc)
    {
        // Desenha a "tecla" estilizada (Keycap)
        Rect rectTecla = new Rect(x, y, 76f, 22f);
        GUI.DrawTexture(rectTecla, texFundoTecla);
        // Pequena borda lateral azul na tecla
        GUI.DrawTexture(new Rect(x, y, 3f, 22f), texBordaCiano);
        GUI.Label(rectTecla, tecla, estiloTecla);

        // Desenha a descricao com suporte a cores (Rich Text)
        GUI.Label(new Rect(x + 84f, y, 270f, 22f), descricao, estiloDesc);
    }

    void OnGUI()
    {
        if (Application.isPlaying && instanciaAtiva != this) return;
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) return;

        InicializarTexturasUI();

        float escalaUI = Mathf.Max(1f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escalaUI, escalaUI, 1f));
        float larguraTela = Screen.width / escalaUI;
        float alturaTela = Screen.height / escalaUI;

        // =====================================================================
        // 1. PLACAR SUPERIOR MODERNO COM BARRA DE PROGRESSO
        // =====================================================================
        Rect rectPlacar = new Rect(larguraTela / 2f - 235f, 14f, 470f, 54f);
        GUI.DrawTexture(rectPlacar, texFundoPainel);
        GUI.DrawTexture(new Rect(rectPlacar.x, rectPlacar.y, rectPlacar.width, 3f), texBordaCiano);

        GUIStyle estiloPlacar = new GUIStyle(GUI.skin.label);
        estiloPlacar.fontSize = 19;
        estiloPlacar.fontStyle = FontStyle.Bold;
        estiloPlacar.alignment = TextAnchor.MiddleCenter;
        estiloPlacar.richText = true;

        string corVidas = (vidas > 1) ? "#FF5577" : "#FF2222";
        string textoPlacar = "<color=#33FF88>CRISTAIS: " + pontuacao + " / " + totalCristais + "</color>      <color=#6688AA>|</color>      <color=" + corVidas + ">VIDAS: " + vidas + "</color>";
        GUI.Label(new Rect(rectPlacar.x, rectPlacar.y + 4f, rectPlacar.width, 32f), textoPlacar, estiloPlacar);

        // Barra de progresso dos 6 cristais
        Rect rectBarraFundo = new Rect(rectPlacar.x + 25f, rectPlacar.y + 39f, rectPlacar.width - 50f, 7f);
        GUI.DrawTexture(rectBarraFundo, texBarraFundo);
        float proporcao = Mathf.Clamp01((float)pontuacao / Mathf.Max(1, totalCristais));
        if (proporcao > 0f)
        {
            GUI.DrawTexture(new Rect(rectBarraFundo.x, rectBarraFundo.y, rectBarraFundo.width * proporcao, rectBarraFundo.height), texBarraProgresso);
        }

        // Aviso flutuante quando pega cristal ou leva dano
        if (tempoFeedback > 0f)
        {
            Rect rectFeedback = new Rect(larguraTela / 2f - 220f, 74f, 440f, 34f);
            GUI.DrawTexture(rectFeedback, texFundoPainel);
            GUI.DrawTexture(new Rect(rectFeedback.x, rectFeedback.y, 4f, rectFeedback.height), texBordaCiano);

            GUIStyle estiloFeedback = new GUIStyle(GUI.skin.label);
            estiloFeedback.fontSize = 16;
            estiloFeedback.fontStyle = FontStyle.Bold;
            estiloFeedback.alignment = TextAnchor.MiddleCenter;
            estiloFeedback.normal.textColor = (jogoVencido || mensagemFeedback.StartsWith("+")) ? new Color(1f, 0.9f, 0.2f) : new Color(1f, 0.4f, 0.4f);
            GUI.Label(rectFeedback, mensagemFeedback, estiloFeedback);
        }

        // =====================================================================
        // 2. QUADRO DE COMANDOS ESTILIZADO (CANTO SUPERIOR ESQUERDO)
        // =====================================================================
        Rect rectPainel = new Rect(14f, 14f, 365f, 206f);
        // Sombra suave atras do painel
        GUI.color = new Color(0f, 0f, 0f, 0.35f);
        GUI.DrawTexture(new Rect(rectPainel.x + 4f, rectPainel.y + 4f, rectPainel.width, rectPainel.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Fundo escuro translucido e barra superior azul neon
        GUI.DrawTexture(rectPainel, texFundoPainel);
        GUI.DrawTexture(new Rect(rectPainel.x, rectPainel.y, rectPainel.width, 3f), texBordaCiano);
        GUI.DrawTexture(new Rect(rectPainel.x, rectPainel.y + 34f, rectPainel.width, 1f), new Texture2D(1, 1));

        // Titulo do Quadro de Comandos
        GUIStyle estiloCabecalho = new GUIStyle(GUI.skin.label);
        estiloCabecalho.fontSize = 13;
        estiloCabecalho.fontStyle = FontStyle.Bold;
        estiloCabecalho.alignment = TextAnchor.MiddleLeft;
        estiloCabecalho.richText = true;
        GUI.Label(new Rect(rectPainel.x + 12f, rectPainel.y + 6f, rectPainel.width - 20f, 24f),
            "<color=#22CCFF>CONTROLES & TECNICAS DE CG</color>", estiloCabecalho);

        // Estilos para os botoes de tecla e descricoes
        GUIStyle estiloTecla = new GUIStyle(GUI.skin.label);
        estiloTecla.fontSize = 12;
        estiloTecla.fontStyle = FontStyle.Bold;
        estiloTecla.alignment = TextAnchor.MiddleCenter;
        estiloTecla.normal.textColor = new Color(0.4f, 0.9f, 1f);

        GUIStyle estiloDesc = new GUIStyle(GUI.skin.label);
        estiloDesc.fontSize = 12;
        estiloDesc.alignment = TextAnchor.MiddleLeft;
        estiloDesc.richText = true;
        estiloDesc.normal.textColor = new Color(0.88f, 0.92f, 0.98f);

        string projecaoAtual = cam.orthographic
            ? "<color=#FFD147>ORTOGRAFICA</color>"
            : "<color=#44FF99>PERSPECTIVA</color>";

        string recorteFar = (cam.farClipPlane < 40f)
            ? "<color=#FF6666>ATIVO (27m)</color>"
            : "<color=#44FF99>NORMAL (100m)</color>";

        string statusBezier = mostrarCurvasBezier
            ? "<color=#44FF99>VISIVEL</color>"
            : "<color=#FF6666>OCULTA</color>";

        float xBase = rectPainel.x + 12f;
        float yBase = rectPainel.y + 42f;
        float passoY = 26f;

        DesenharLinhaComando(xBase, yBase + passoY * 0, "WASD", "Translacao 3D do Carro", estiloTecla, estiloDesc);
        DesenharLinhaComando(xBase, yBase + passoY * 1, "Tecla P", "Projecao: " + projecaoAtual, estiloTecla, estiloDesc);
        DesenharLinhaComando(xBase, yBase + passoY * 2, "Tecla C", "Recorte 3D: " + recorteFar, estiloTecla, estiloDesc);
        DesenharLinhaComando(xBase, yBase + passoY * 3, "Tecla B", "Curva de Bezier: " + statusBezier, estiloTecla, estiloDesc);
        DesenharLinhaComando(xBase, yBase + passoY * 4, "Segure L", "Girar Luz <color=#AACCFF>(Phong / Lambert)</color>", estiloTecla, estiloDesc);
        DesenharLinhaComando(xBase, yBase + passoY * 5, "ESC", "<color=#FF8888>Sair / Fechar o Jogo</color>", estiloTecla, estiloDesc);

        // =====================================================================
        // 3. TELA DE VITORIA OU FIM DE JOGO
        // =====================================================================
        if (jogoVencido || jogoPerdido)
        {
            Rect rectFim = new Rect(larguraTela / 2f - 260f, alturaTela / 2f - 65f, 520f, 130f);
            GUI.DrawTexture(rectFim, texFundoPainel);
            GUI.DrawTexture(new Rect(rectFim.x, rectFim.y, rectFim.width, 4f), jogoVencido ? texBarraProgresso : texBordaCiano);

            GUIStyle estiloFim = new GUIStyle(GUI.skin.label);
            estiloFim.fontSize = 22;
            estiloFim.fontStyle = FontStyle.Bold;
            estiloFim.alignment = TextAnchor.MiddleCenter;
            estiloFim.richText = true;

            string msg = jogoVencido
                ? "<color=#33FF88>VITORIA! TODOS OS 6 CRISTAIS COLETADOS!</color>\n\n<size=16><color=#FFFFFF>[ Aperte R para Jogar Novamente ]</color></size>"
                : "<color=#FF4455>FIM DE JOGO! VOCE PERDEU AS VIDAS!</color>\n\n<size=16><color=#FFFFFF>[ Aperte R para Reiniciar ]</color></size>";

            GUI.Label(rectFim, msg, estiloFim);
        }
    }
}
