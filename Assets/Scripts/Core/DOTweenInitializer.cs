using UnityEngine;
using DG.Tweening;

namespace AnimaParty.Core
{
    public class DOTweenInitializer : MonoBehaviour
    {
        void Awake()
        {
            // Configurar DOTween globalmente
            DOTween.Init(recycleAllByDefault: false, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly);
            DOTween.defaultEaseType = Ease.OutQuad;
            DOTween.SetTweensCapacity(200, 50);
            
            Debug.Log("DOTween inicializado correctamente");
        }
        
        void OnDestroy()
        {
            // Limpiar todos los tweens al salir
            DOTween.KillAll();
        }
    }
}