import { BookOpenIcon } from "lucide-react"
import { Alert, AlertTitle, AlertDescription } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import {
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from "@/components/ui/empty"
import type { confidence } from "./loans-api"

export function LoanReadError({
  initial = false,
  isLoading,
  retry,
}: {
  initial?: boolean
  isLoading: boolean
  retry: () => void
}) {
  return (
    <Alert variant="destructive">
      <AlertTitle>
        {initial
          ? "Impossible de charger les prêts"
          : "Impossible d’actualiser les prêts"}
      </AlertTitle>
      <AlertDescription>
        <p>
          {initial
            ? "Réessayez dans quelques instants."
            : "Les dernières données reçues restent affichées."}
        </p>
        <Button variant="outline" onClick={retry} disabled={isLoading}>
          {isLoading ? "Chargement…" : "Réessayer"}
        </Button>
      </AlertDescription>
    </Alert>
  )
}

export function LoanCoverageAlert({
  hasConfiguredAccounts,
  scope,
}: {
  hasConfiguredAccounts: boolean
  scope: ReturnType<typeof confidence>
}) {
  if (hasConfiguredAccounts && scope.reliable) return null

  let title = "Dernier état connu des prêts"
  let description =
    "Les données affichées ne sont pas confirmées à jour. Consultez le détail par réseau ci-dessous."
  if (!hasConfiguredAccounts) {
    title = "Aucun compte de bibliothèque configuré"
    description =
      "Ajoutez un compte de bibliothèque dans la configuration du serveur pour afficher les prêts."
  } else if (scope.unavailable) {
    title = "Données des prêts indisponibles"
    description =
      "Les prêts de ces comptes n’ont pas encore pu être vérifiés. Les données déjà enregistrées restent affichées."
  } else if (!scope.complete) {
    title = "Liste des prêts incomplète"
    description =
      "Les prêts de certains comptes n’ont pas encore pu être vérifiés. La liste et les compteurs peuvent être incomplets."
  }
  return (
    <Alert>
      <AlertTitle>{title}</AlertTitle>
      <AlertDescription>{description}</AlertDescription>
    </Alert>
  )
}

export function LoanEmptyState({
  hasConfiguredAccounts,
  unavailable,
  reliable,
  hasFilters,
  clearFilters,
}: {
  hasConfiguredAccounts: boolean
  unavailable: boolean
  reliable: boolean
  hasFilters: boolean
  clearFilters: () => void
}) {
  let title = "Aucun prêt en cours"
  let description = "Aucun prêt signalé lors de la dernière mise à jour."
  if (!hasConfiguredAccounts) {
    title = "Configurez vos comptes pour afficher les prêts"
    description =
      "Ajoutez un compte de bibliothèque dans la configuration du serveur."
  } else if (!reliable) {
    title = unavailable
      ? "Données des prêts indisponibles"
      : hasFilters
        ? "Aucun prêt connu ne correspond aux filtres"
        : "Aucun prêt dans les données disponibles"
    description =
      "Les données ne sont pas confirmées à jour : des prêts peuvent manquer dans cette liste."
  } else if (hasFilters) {
    title = "Aucun prêt ne correspond aux filtres"
    description =
      "Modifiez ou effacez les filtres pour afficher les autres prêts."
  }
  return (
    <Empty>
      <EmptyHeader>
        <EmptyMedia variant="icon">
          <BookOpenIcon aria-hidden="true" />
        </EmptyMedia>
        <EmptyTitle>
          <h2>{title}</h2>
        </EmptyTitle>
        <EmptyDescription>{description}</EmptyDescription>
      </EmptyHeader>
      {hasFilters && (
        <EmptyContent>
          <Button variant="outline" onClick={clearFilters}>
            Effacer les filtres
          </Button>
        </EmptyContent>
      )}
    </Empty>
  )
}
