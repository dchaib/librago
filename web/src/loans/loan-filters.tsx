import { useId } from "react"

import { Checkbox } from "@/components/ui/checkbox"
import {
  Field,
  FieldGroup,
  FieldLabel,
  FieldLegend,
  FieldSet,
} from "@/components/ui/field"
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { severities, severityLabels, type Filters, type Option } from "./loans"

const allValue = "__all__"

function Choice({
  label,
  all,
  options,
  value,
  onChange,
}: {
  label: string
  all: string
  options: Option[]
  value: string
  onChange: (value: string) => void
}) {
  const id = useId()
  const items = [
    { label: all, value: allValue },
    ...options.map((option) => ({ label: option.name, value: option.id })),
  ]

  return (
    <Field>
      <FieldLabel htmlFor={id}>{label}</FieldLabel>
      <Select
        items={items}
        value={value || allValue}
        onValueChange={(next) =>
          onChange(next === allValue || next === null ? "" : next)
        }
      >
        <SelectTrigger id={id}>
          <SelectValue />
        </SelectTrigger>
        <SelectContent alignItemWithTrigger={false}>
          <SelectGroup>
            {items.map((item) => (
              <SelectItem key={item.value} value={item.value}>
                {item.label}
              </SelectItem>
            ))}
          </SelectGroup>
        </SelectContent>
      </Select>
    </Field>
  )
}

export function LoanFilters({
  value,
  onChange,
  mobile = false,
  networks,
  borrowers,
}: {
  value: Filters
  onChange: (filters: Filters) => void
  mobile?: boolean
  networks: Option[]
  borrowers: Option[]
}) {
  const id = useId()
  const summary = value.severities.length
    ? `${value.severities.length} niveau${value.severities.length > 1 ? "x" : ""} sélectionné${value.severities.length > 1 ? "s" : ""}`
    : "Toutes les échéances"
  const severityItems = [
    { value: allValue, label: "Toutes les échéances" },
    ...severities.map((severity) => ({
      value: severity,
      label: severityLabels[severity],
    })),
  ]

  return (
    <FieldGroup className={mobile ? undefined : "grid grid-cols-3 items-start"}>
      <Choice
        label="Réseau"
        all="Tous les réseaux"
        options={networks}
        value={value.network}
        onChange={(network) => onChange({ ...value, network })}
      />
      <Choice
        label="Emprunteur"
        all="Tous les emprunteurs"
        options={borrowers}
        value={value.borrower}
        onChange={(borrower) => onChange({ ...value, borrower })}
      />
      {mobile ? (
        <FieldSet>
          <FieldLegend variant="label">Échéance</FieldLegend>
          <FieldGroup data-slot="checkbox-group">
            {severities.map((severity) => (
              <Field key={severity} orientation="horizontal">
                <Checkbox
                  id={`${id}-${severity}`}
                  checked={value.severities.includes(severity)}
                  onCheckedChange={(checked) =>
                    onChange({
                      ...value,
                      severities: checked
                        ? [...value.severities, severity]
                        : value.severities.filter(
                            (entry) => entry !== severity
                          ),
                    })
                  }
                />
                <FieldLabel htmlFor={`${id}-${severity}`}>
                  {severityLabels[severity]}
                </FieldLabel>
              </Field>
            ))}
          </FieldGroup>
        </FieldSet>
      ) : (
        <Field>
          <FieldLabel htmlFor={id}>Échéance</FieldLabel>
          <Select
            multiple
            items={severityItems}
            value={value.severities.length ? value.severities : [allValue]}
            onValueChange={(selected) =>
              onChange({
                ...value,
                severities:
                  value.severities.length && selected.includes(allValue)
                    ? []
                    : severities.filter((severity) =>
                        selected.includes(severity)
                      ),
              })
            }
          >
            <SelectTrigger
              id={id}
              aria-label={`Filtrer par échéance : ${summary}`}
            >
              <SelectValue>{summary}</SelectValue>
            </SelectTrigger>
            <SelectContent alignItemWithTrigger={false}>
              <SelectGroup>
                {severityItems.map((item) => (
                  <SelectItem key={item.value} value={item.value}>
                    {item.label}
                  </SelectItem>
                ))}
              </SelectGroup>
            </SelectContent>
          </Select>
        </Field>
      )}
    </FieldGroup>
  )
}
