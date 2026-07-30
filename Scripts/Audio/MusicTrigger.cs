using UnityEngine;

public class MusicTrigger : MonoBehaviour
{
    public MusicSystem sistemaCentral;
    public AudioClip cancionDeEstaZona;
    public bool usarSoloUnaVez = true;
    private bool yaSeActivo = false;

    void OnTriggerEnter(Collider other)
    {
        if (yaSeActivo && usarSoloUnaVez) return;

        if (other.CompareTag("Player"))
        {
            sistemaCentral.ChangeMusic(cancionDeEstaZona);
            yaSeActivo = true;
            if (usarSoloUnaVez) this.gameObject.SetActive(false);
        }
    }
}