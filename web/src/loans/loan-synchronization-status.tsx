import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { isCurrent, type LoansResponse } from "./loans-api"

export function LoanSynchronizationStatus({
  synchronizations,
  networks,
  timeZone,
  now,
}: Pick<LoansResponse, "synchronizations" | "networks" | "timeZone"> & {
  now: number
}) {
  return (
    <footer>
      <Card>
        <CardHeader>
          <CardTitle>
            <h2>Dernière mise à jour complète des prêts</h2>
          </CardTitle>
          <CardDescription>Fuseau horaire : {timeZone}</CardDescription>
        </CardHeader>
        <CardContent>
          <dl className="grid gap-4 sm:grid-cols-2">
            {synchronizations.map((state) => (
              <div key={state.networkId}>
                <dt className="font-medium">
                  {networks.find((n) => n.id === state.networkId)?.name}
                </dt>
                <dd className="text-muted-foreground">
                  {state.lastCompleteSuccessAt ? (
                    <time dateTime={state.lastCompleteSuccessAt}>
                      {new Intl.DateTimeFormat("fr-FR", {
                        timeZone,
                        dateStyle: "short",
                        timeStyle: "short",
                      }).format(new Date(state.lastCompleteSuccessAt))}
                    </time>
                  ) : (
                    "Aucune mise à jour complète"
                  )}
                  {!isCurrent(state, now) && (
                    <p>
                      {state.coverage === "unavailable"
                        ? "Données non vérifiées"
                        : state.coverage === "partial"
                          ? "Données incomplètes"
                          : "Dernier état connu"}
                      {state.result === "failed"
                        ? " · Dernière mise à jour échouée"
                        : state.result === "partial"
                          ? " · Dernière mise à jour incomplète"
                          : ""}
                      {!state.hasCurrentAccountCoverage &&
                        " · Mise à jour requise pour les comptes actuels"}
                      {(state.isStale ||
                        (state.lastCompleteSuccessAt &&
                          now - Date.parse(state.lastCompleteSuccessAt) >
                            86_400_000)) &&
                        " · Dernière mise à jour complète il y a plus de 24 h"}
                    </p>
                  )}
                </dd>
              </div>
            ))}
          </dl>
        </CardContent>
      </Card>
    </footer>
  )
}
