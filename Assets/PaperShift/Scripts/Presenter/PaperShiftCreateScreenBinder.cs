using PaperShift.Domain;
using PaperShift.Model;
using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftCreateScreenBinder : PaperShiftScreenBinderBase
    {
        public PaperShiftCreateWorkerViewReferences View;

        [HideInInspector] public PaperShiftTextBinding[] Texts = new PaperShiftTextBinding[0];
        [HideInInspector] public PaperShiftEraTileBinding[] EraTiles = new PaperShiftEraTileBinding[0];
        [HideInInspector] public Button RandomButton;
        [HideInInspector] public Button RandomNameButton;
        [HideInInspector] public Button PlayButton;
        [HideInInspector] public Button CustomButton;
        [HideInInspector] public Button NextButton;

        private void Reset()
        {
            Screen = PaperShiftScreen.Create;
            View = GetComponent<PaperShiftCreateWorkerViewReferences>();
        }

        public override void BindActions()
        {
            ResolveView();
            Bind(ActiveRandomButton(), Randomize);
            Bind(ActiveRandomNameButton(), Randomize);
            Bind(ActivePlayButton(), BeginWorkerFlow);
            Bind(ActiveCustomButton(), BeginWorkerFlow);
            Bind(ActiveNextButton(), BeginWorkerFlow);
        }

        public override void RefreshView()
        {
            if (State == null || State.Worker == null)
            {
                return;
            }

            ResolveView();

            var worker = State.Worker;
            if (View != null)
            {
                SetActive(View.CoinText == null ? null : View.CoinText.gameObject, false);
                Set(View.LastNameText, worker.LastName);
                Set(View.FirstNameText, worker.FirstName);
                Set(View.FullNameText, worker.FullName);
                Set(View.GenderText, PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Gender));
                Set(View.PersonalityText, PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Personality));
                Set(View.AgeText, PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Age));
                Set(View.EraText, worker.EraName);
                RefreshAttributes(View.Attributes);
                RefreshEraTiles(View.EraTiles);
            }

            RefreshLegacyTexts(worker);
            RefreshEraTiles(EraTiles);
        }

        private void ResolveView()
        {
            if (View == null)
            {
                View = GetComponent<PaperShiftCreateWorkerViewReferences>();
            }
        }

        private void RefreshAttributes(PaperShiftCreateAttributeView[] attributes)
        {
            if (attributes == null)
            {
                return;
            }

            for (var i = 0; i < attributes.Length; i++)
            {
                var item = attributes[i];
                if (item == null || string.IsNullOrEmpty(item.FieldId))
                {
                    continue;
                }

                var fieldId = PaperShiftWorkerAttributes.Canonicalize(item.FieldId);
                Set(item.LabelText, PaperShiftWorkerAttributes.DisplayName(fieldId));
                Set(item.ValueText, PaperShiftWorkerAttributes.DisplayValue(State.Worker, fieldId));
                ApplyRarityState(item.NormalState, item.RareState, item.SuperRareState, PaperShiftWorkerAttributes.RarityId(State.Worker, fieldId));
            }
        }

        private void RefreshLegacyTexts(WorkerProfile worker)
        {
            Set(Texts, "gender", PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Gender));
            Set(Texts, "personality", PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Personality));
            Set(Texts, "age", PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Age));
            Set(Texts, "era", worker.EraName);
            Set(Texts, "height", PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Height));
            Set(Texts, "appearance", PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Appearance));
            Set(Texts, "education", PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Education));
            Set(Texts, "family", PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Family));
            Set(Texts, "major", PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Major));
            Set(Texts, "ability", PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Ability));
            Set(Texts, "advantage", PaperShiftWorkerAttributes.BestAttributeLabel(worker) + " / " + WorkerTagSummary(worker));
            HideTextBindingRow(Texts, "asset");
            HideTextBindingRow(Texts, "coin");
            Set(Texts, "lastName", worker.LastName);
            Set(Texts, "firstName", worker.FirstName);
        }

        private void HideTextBindingRow(PaperShiftTextBinding[] bindings, string id)
        {
            var binding = FindBinding(bindings, id);
            if (binding == null || binding.Text == null)
            {
                return;
            }

            var row = binding.Text.transform.parent;
            if (row != null)
            {
                row.gameObject.SetActive(false);
                return;
            }

            binding.Text.gameObject.SetActive(false);
        }

        private void RefreshEraTiles(PaperShiftEraTileBinding[] tiles)
        {
            if (tiles == null)
            {
                return;
            }

            for (var i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];
                if (tile == null || string.IsNullOrEmpty(tile.EraId))
                {
                    continue;
                }

                SetEraTileActive(tile, false);
            }
        }

        private static void SetEraTileActive(PaperShiftEraTileBinding tile, bool active)
        {
            if (tile.Button != null)
            {
                tile.Button.gameObject.SetActive(active);
            }

            if (tile.Background != null)
            {
                tile.Background.gameObject.SetActive(active);
            }

            if (tile.Label != null)
            {
                tile.Label.gameObject.SetActive(active);
            }
        }

        private void Randomize()
        {
            Presenter.RandomizeWorker();
            SceneController.ShowCreate();
            RefreshAll();
        }

        private void BeginWorkerFlow()
        {
            Presenter.RollTagsAndShow();
            RefreshAll();
        }

        private Button ActiveRandomButton()
        {
            return View != null && View.RandomButton != null ? View.RandomButton : RandomButton;
        }

        private Button ActiveRandomNameButton()
        {
            return View != null && View.RandomNameButton != null ? View.RandomNameButton : RandomNameButton;
        }

        private Button ActivePlayButton()
        {
            return View != null && View.PlayButton != null ? View.PlayButton : PlayButton;
        }

        private Button ActiveCustomButton()
        {
            return View != null && View.CustomButton != null ? View.CustomButton : CustomButton;
        }

        private Button ActiveNextButton()
        {
            return View != null && View.NextButton != null ? View.NextButton : NextButton;
        }

        private static void ApplyRarityState(GameObject normal, GameObject rare, GameObject superRare, string rarityId)
        {
            var isRare = string.Equals(rarityId, "rare", System.StringComparison.OrdinalIgnoreCase);
            var isSuperRare = string.Equals(rarityId, "super_rare", System.StringComparison.OrdinalIgnoreCase);
            SetActive(normal, !isRare && !isSuperRare);
            SetActive(rare, isRare);
            SetActive(superRare, isSuperRare);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }
}
