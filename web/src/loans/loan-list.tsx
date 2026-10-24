import { useState } from "react"
import { BookOpenIcon, LandmarkIcon, UserRoundIcon } from "lucide-react"

import {
  Item,
  ItemContent,
  ItemDescription,
  ItemGroup,
  ItemMedia,
  ItemTitle,
} from "@/components/ui/item"
import {
  Table,
  TableBody,
  TableCaption,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  formatDueDate,
  relativeDeadline,
  remainingDays,
  type Loan,
} from "./loans"
import { borrowers, networks } from "./mock-loans"

function networkName(id: string) {
  return networks.find((network) => network.id === id)?.name ?? id
}

function borrowerName(id: string) {
  return borrowers.find((borrower) => borrower.id === id)?.name ?? id
}

function Cover({ loan }: { loan: Loan }) {
  const [failedSource, setFailedSource] = useState<string>()
  const missing = !loan.cover || loan.cover === failedSource

  return (
    <div className="flex h-21 w-14 shrink-0 items-center justify-center overflow-hidden rounded-sm border">
      {missing ? (
        <BookOpenIcon
          role="img"
          aria-label="Couverture indisponible"
          className="size-4"
        />
      ) : (
        <img
          src={loan.cover}
          alt=""
          width="56"
          height="84"
          loading="lazy"
          className="h-full w-full object-cover"
          onError={() => setFailedSource(loan.cover)}
        />
      )}
    </div>
  )
}

function Deadline({ loan, today }: { loan: Loan; today: string }) {
  const days = remainingDays(loan.dueDate, today)
  return (
    <div className="flex min-w-0 flex-col gap-1 text-right">
      <p className="text-sm font-medium">{relativeDeadline(days)}</p>
      <time dateTime={loan.dueDate} className="text-sm text-muted-foreground">
        <span className="sr-only">Retour au plus tard le </span>
        {formatDueDate(loan.dueDate)}
      </time>
    </div>
  )
}

export function LoanList({ loans, today }: { loans: Loan[]; today: string }) {
  return (
    <>
      <ItemGroup
        aria-label="Prêts par date de retour croissante"
        className="md:hidden"
      >
        {loans.map((loan) => (
          <Item
            key={loan.id}
            role="listitem"
            variant="outline"
            className="flex-nowrap items-start"
          >
            <Cover loan={loan} />
            <ItemContent className="min-w-0 wrap-anywhere">
              <div className="grid grid-cols-[minmax(0,1fr)_minmax(0,40%)] items-start gap-2">
                <ItemContent className="min-w-0">
                  <ItemTitle className="line-clamp-none">
                    <h2>{loan.title}</h2>
                  </ItemTitle>
                  {loan.author && (
                    <ItemDescription>{loan.author}</ItemDescription>
                  )}
                </ItemContent>
                <Deadline loan={loan} today={today} />
              </div>
              <div className="flex flex-wrap items-start gap-x-3 gap-y-1">
                <div className="flex min-w-0 items-start gap-1">
                  <ItemMedia variant="icon">
                    <LandmarkIcon aria-hidden="true" />
                  </ItemMedia>
                  <ItemDescription className="line-clamp-none min-w-0">
                    <span className="sr-only">Réseau : </span>
                    {networkName(loan.networkId)}
                  </ItemDescription>
                </div>
                <div className="flex min-w-0 items-start gap-1">
                  <ItemMedia variant="icon">
                    <UserRoundIcon aria-hidden="true" />
                  </ItemMedia>
                  <ItemDescription className="line-clamp-none min-w-0">
                    <span className="sr-only">Emprunteur : </span>
                    {borrowerName(loan.borrowerId)}
                  </ItemDescription>
                </div>
              </div>
            </ItemContent>
          </Item>
        ))}
      </ItemGroup>
      <div className="hidden md:block">
        <Table className="table-fixed">
          <TableCaption className="sr-only">
            Prêts de toute la famille, par date de retour croissante
          </TableCaption>
          <TableHeader>
            <TableRow>
              <TableHead scope="col">Document</TableHead>
              <TableHead scope="col" className="w-40">
                Réseau
              </TableHead>
              <TableHead scope="col" className="w-40">
                Emprunteur
              </TableHead>
              <TableHead scope="col" className="w-40 text-right">
                Échéance
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loans.map((loan) => (
              <TableRow key={loan.id}>
                <TableCell className="whitespace-normal">
                  <Item size="xs" className="flex-nowrap">
                    <Cover loan={loan} />
                    <ItemContent className="min-w-0 wrap-anywhere">
                      <ItemTitle className="line-clamp-none">
                        {loan.title}
                      </ItemTitle>
                      {loan.author && (
                        <ItemDescription>{loan.author}</ItemDescription>
                      )}
                    </ItemContent>
                  </Item>
                </TableCell>
                <TableCell className="whitespace-normal">
                  {networkName(loan.networkId)}
                </TableCell>
                <TableCell className="whitespace-normal">
                  {borrowerName(loan.borrowerId)}
                </TableCell>
                <TableCell>
                  <div className="flex justify-end">
                    <Deadline loan={loan} today={today} />
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </>
  )
}
